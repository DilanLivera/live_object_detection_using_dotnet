window.CameraManager = {
    // Stores active camera streams mapped by their video element IDs
    activeStreams: new Map(),

    COMPRESSION: {
        quality: 0.75, // 70% quality for transfer to prevent SignalR issues
        useGzip: true, // Enable GZIP compression
    },

    /**
     * Initializes the camera stream for a specified video element.
     * @param {string} videoElementId - ID of the video element to attach the camera stream to
     * @returns {Promise<boolean>} True if camera initialization succeeds, false otherwise
     */
    async initializeCamera(videoElementId) {
        const cameraInitialized = true;

        const videoElement = document.getElementById(videoElementId);

        if (!videoElement) {
            throw new Error("Video element not found");
        }

        try {
            const video = {
                facingMode: "environment",
            };
            const stream = await navigator.mediaDevices.getUserMedia({video});

            videoElement.srcObject = stream;
            this.activeStreams.set(videoElementId, stream);

            return new Promise((resolve) => {
                videoElement.onloadedmetadata = () => videoElement.play()
                    .then(() => resolve(cameraInitialized))
                    .catch((error) => {
                        console.error("Error playing video:", error);
                        resolve(!cameraInitialized);
                    });
            });
        } catch (error) {
            console.error("Error initializing camera:", error);
            return !cameraInitialized;
        }
    },

    /**
     * Stops the camera stream and cleans up resources.
     * @param {string} videoElementId - ID of the video element whose stream should be stopped
     */
    stopCamera(videoElementId) {
        const videoElement = document.getElementById(videoElementId);

        if (!videoElement || !videoElement.srcObject) {
            throw new Error("Video element or source object is null");
        }

        const tracks = videoElement.srcObject.getTracks();
        tracks.forEach((track) => track.stop());
        videoElement.srcObject = null;
        this.activeStreams.delete(videoElementId);
    },

    /**
     * Captures a single frame from the video stream and returns it as a byte array.
     * @param {string} videoElementId - ID of the video element to capture from
     * @returns {Promise<{data: Uint8Array, isCompressed: boolean}|null>} Byte array and compression flag
     */
    async captureFrame(videoElementId) {
        const videoElement = document.getElementById(videoElementId);

        if (!videoElement) {
            throw new Error("Video element not found");
        }

        // Check if video is ready
        if (!videoElement.videoWidth || !videoElement.videoHeight) {
            throw new Error("Video dimensions not available yet");
        }

        if (videoElement.readyState !== videoElement.HAVE_ENOUGH_DATA) {
            throw new Error("Video not ready for capture");
        }

        const captureCanvas = document.createElement("canvas");
        const canvasRenderingContext2D = captureCanvas.getContext("2d");

        if (!canvasRenderingContext2D) {
            throw new Error("Failed to get 2D rendering context");
        }

        try {
            captureCanvas.width = videoElement.videoWidth;
            captureCanvas.height = videoElement.videoHeight;

            const dx = 0, dy = 0;
            canvasRenderingContext2D.drawImage(videoElement, dx, dy);

            const type = "image/jpeg";
            const blob = await new Promise((resolve) => captureCanvas.toBlob(resolve, type, this.COMPRESSION.quality));

            console.assert(blob != null, {message: "A blob object for the image contained in the canvas must be created"});
            if (!blob) {
                throw new Error("Failed to create blob");
            }

            const arrayBuffer = await blob.arrayBuffer();
            const imageInBytes = new Uint8Array(arrayBuffer);
            const shouldCompress = this.COMPRESSION.useGzip && window.pako != null;
            const finalImageInBytes = shouldCompress ? this.compressData(imageInBytes) : imageInBytes;

            console.debug("image details: ", {
                imageDimensions: {
                    height: captureCanvas.height,
                    width: captureCanvas.width,
                },
                imageQuality: this.COMPRESSION.quality,
                ...(shouldCompress && {
                    imageCompression: {
                        isGzipEnabled: this.COMPRESSION.useGzip,
                        originalSizeInKb: `${(imageInBytes.length / 1024).toFixed(2)}KB`,
                        compressedSizeInKb: `${(finalImageInBytes.length / 1024).toFixed(2)}KB`,
                        reductionAsPercentage: `${(100 - (finalImageInBytes.length / imageInBytes.length) * 100).toFixed(1)}% reduction`,
                    }
                })
            });

            return {
                data: finalImageInBytes,
                isCompressed: shouldCompress,
                imageHeight: captureCanvas.height,
                imageWidth: captureCanvas.width,
                imageQuality: this.COMPRESSION.quality
            };
        } catch (error) {
            console.error("Error capturing frame:", error);
            return null;
        }
    },

    /**
     * Clears all content from the specified canvas.
     * @param {string} canvasId - ID of the canvas to clear
     */
    clearCanvas(canvasId) {
        const canvas = document.getElementById(canvasId);

        if (canvas == null) {
            throw new Error("Canvas element not found");
        }

        const contextId = "2d";
        const canvasRenderingContext2D = canvas.getContext(contextId);

        if (canvasRenderingContext2D == null) {
            throw new Error("Failed to get 2D rendering context");
        }
        const x = 0, y = 0;
        canvasRenderingContext2D.clearRect(x, y, canvas.width, canvas.height);
    },

    /**
     * Draws object detection results on the specified canvas.
     * @param {string} canvasId - ID of the canvas to draw on
     * @param {string} videoElementId - ID of the video element to get dimensions from
     * @param {Array<{label: string, confidence: number, boundingBox: {x: number, y: number, width: number, height: number}}>} detections - Array of detection results
     */
    drawDetections(canvasId, videoElementId, detections) {
        const STYLES = {
            lineWidth: 2,
            font: "16px Arial",
            boxColor: "#00ff00",
            textColor: "#ffffff",
            textHeight: 20,
            labelPadding: 5,
            labelBackgroundAlpha: 0.7,
        };

        const canvas = document.getElementById(canvasId);

        if (canvas == null) {
            throw new Error("Canvas element not found");
        }

        const video = document.getElementById(videoElementId);

        if (video == null) {
            throw new Error(`Video element with ID '${videoElementId}' not found`);
        }

        // canvas.width/height sets the canvas element's drawing surface
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        // canvas.style.width/height controls the display size in the browser
        canvas.style.width = "100%";
        canvas.style.height = "auto";

        const canvasRenderingContext2D = canvas.getContext("2d");
        if (!canvasRenderingContext2D) {
            throw new Error("Failed to get 2D rendering context");
        }

        try {
            const x = 0, y = 0;
            canvasRenderingContext2D.clearRect(x, y, canvas.width, canvas.height);

            console.debug(`Dimensions for #${canvasId} canvas and #${videoElementId} video:`, {
                canvas: `${canvas.width}x${canvas.height}`,
                videoSource: `${video.videoWidth}x${video.videoHeight}`,
                videoDisplay: `${video.clientWidth}x${video.clientHeight}`
            });

            canvasRenderingContext2D.lineWidth = STYLES.lineWidth;
            canvasRenderingContext2D.font = STYLES.font;
            canvasRenderingContext2D.textBaseline = "top";

            detections.forEach((detection) => {
                const {label, confidence, boundingBox} = detection;

                console.debug("Processing detection:", {
                    label,
                    confidence,
                    "originalBox:": boundingBox
                });

                const displayBox = {
                    x: Math.max(0, Math.min(canvas.width - 1, boundingBox.x)),
                    y: Math.max(0, Math.min(canvas.height - 1, boundingBox.y)),
                    width: Math.max(1, Math.min(canvas.width, boundingBox.width)),
                    height: Math.max(1, Math.min(canvas.height, boundingBox.height))
                };

                // Ensure box doesn't go outside canvas boundaries
                if (displayBox.x + displayBox.width > canvas.width) {
                    displayBox.width = canvas.width - displayBox.x;
                }
                if (displayBox.y + displayBox.height > canvas.height) {
                    displayBox.height = canvas.height - displayBox.y;
                }

                console.debug("Final display box:", displayBox);

                this.drawBoundingBox(canvasRenderingContext2D, displayBox, STYLES);
                this.drawLabel(
                    canvasRenderingContext2D,
                    label,
                    confidence,
                    displayBox,
                    STYLES);
            });
        } catch (error) {
            console.error("Error drawing detections:", error);
        }
    },

    /**
     * Draw a bounding box
     * @param {CanvasRenderingContext2D} canvasRenderingContext2D - Canvas context
     * @param {Object} scaledBox - Box coordinates and dimensions
     * @param {Object} styles - Styling options
     */
    drawBoundingBox(canvasRenderingContext2D, scaledBox, styles) {
        canvasRenderingContext2D.strokeStyle = styles.boxColor;
        canvasRenderingContext2D.strokeRect(
            scaledBox.x,
            scaledBox.y,
            scaledBox.width,
            scaledBox.height
        );
    },

    /**
     * Draw label
     * @param {CanvasRenderingContext2D} canvasRenderingContext2D - Canvas context
     * @param {string} label - Detection label
     * @param {number} confidence - Detection confidence
     * @param {Object} boundingBox - Box coordinates and dimensions
     * @param {Object} styles - Styling options
     */
    drawLabel(canvasRenderingContext2D, label, confidence, boundingBox, styles) {
        const labelText = `${label} (${(confidence * 100).toFixed(1)}%)`;
        const textMetrics = canvasRenderingContext2D.measureText(labelText);

        canvasRenderingContext2D.fillStyle = `rgba(0, 0, 0, ${styles.labelBackgroundAlpha})`;
        canvasRenderingContext2D.fillRect(
            boundingBox.x,
            boundingBox.y - styles.textHeight,
            textMetrics.width + styles.labelPadding * 2,
            styles.textHeight);

        canvasRenderingContext2D.fillStyle = styles.textColor;
        canvasRenderingContext2D.fillText(
            labelText,
            boundingBox.x + styles.labelPadding,
            boundingBox.y - styles.textHeight + styles.labelPadding);
    },

    /**
     * Compresses data using GZIP
     * @param {Uint8Array} data - Data to compress
     * @returns {Uint8Array} Compressed data
     */
    compressData(data) {
        try {
            return window.pako.gzip(data);
        } catch (error) {
            console.error("Compression failed:", error);
            return data;
        }
    },
};
