window.CameraManager = {
    // Stores active camera streams mapped by their video element IDs
    activeStreams: new Map(),

    PROCESSED_DIMENSIONS: {
        width: 800, // Width used for both capture and detection
        height: 450, // Height used for both capture and detection
        jpegQuality: 0.5, // JPEG quality set to 0.5 (50%) as higher quality causes processing errors
    },

    /**
     * Initializes the camera stream for a specified video element.
     * @param {string} videoElementId - ID of the video element to attach the camera stream to
     * @returns {Promise<boolean>} True if camera initialization succeeds, false otherwise
     */
    async initializeCamera(videoElementId) {
        const cameraInitialized = true;

        const videoElement = document.getElementById(videoElementId);

        if (videoElement == null) {
            throw new Error("Video element not found");
        }

        try {
            const video = {
                facingMode: "environment",
                width: {ideal: 1280},
                height: {ideal: 720},
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

        if (videoElement == null || videoElement.srcObject == null) {
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
     * @returns {Promise<Uint8Array|null>} Byte array of the captured frame or null if capture fails
     */
    async captureFrame(videoElementId) {
        const videoElement = document.getElementById(videoElementId);

        if (!videoElement) {
            console.error("Video element not found");
            return null;
        }

        // Check if video is ready
        if (!videoElement.videoWidth || !videoElement.videoHeight) {
            console.error("Video dimensions not available yet");
            return null;
        }

        if (videoElement.readyState !== videoElement.HAVE_ENOUGH_DATA) {
            console.error("Video not ready for capture");
            return null;
        }

        const captureCanvas = document.createElement("canvas");

        if (captureCanvas == null) {
            throw new Error("Failed to create canvas element");
        }

        captureCanvas.width = this.PROCESSED_DIMENSIONS.width;
        captureCanvas.height = this.PROCESSED_DIMENSIONS.height;

        const contextId = "2d";
        const canvasRenderingContext2D = captureCanvas.getContext(contextId);

        if (canvasRenderingContext2D == null) {
            throw new Error("Failed to get 2D rendering context");
        }

        try {
            const dx = 0, dy = 0;
            canvasRenderingContext2D.drawImage(
                videoElement,
                dx,
                dy,
                this.PROCESSED_DIMENSIONS.width,
                this.PROCESSED_DIMENSIONS.height);

            const type = "image/jpeg";
            const blob = await new Promise(
                // JPEG quality set to 0.5 (50%) as higher quality at 800x450 resolution causes processing errors
                (resolve) => captureCanvas.toBlob(resolve, type, this.PROCESSED_DIMENSIONS.jpegQuality));

            if (blob == null) {
                throw new Error("Failed to create blob from canvas");
            }

            const arrayBuffer = await blob.arrayBuffer();
            return new Uint8Array(arrayBuffer);
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
     * @param {Array<{label: string, confidence: number, box: {x: number, y: number, width: number, height: number}}>} detections - Array of detection results
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

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        const contextId = "2d";
        const canvasRenderingContext2D = canvas.getContext(contextId);

        if (canvasRenderingContext2D == null) {
            throw new Error("Failed to get 2D rendering context");
        }

        try {

            const x = 0, y = 0;
            canvasRenderingContext2D.clearRect(x, y, canvas.width, canvas.height);

            const scale = {
                x: canvas.width / this.PROCESSED_DIMENSIONS.width,
                y: canvas.height / this.PROCESSED_DIMENSIONS.height,
            };

            canvasRenderingContext2D.lineWidth = STYLES.lineWidth;
            canvasRenderingContext2D.font = STYLES.font;
            canvasRenderingContext2D.textBaseline = "top";

            detections.forEach((detection, index) => {
                const {label, confidence, box} = detection;
                const {x, y, width, height} = box;

                const scaledBox = {
                    x: x * scale.x,
                    y: y * scale.y,
                    width: width * scale.x,
                    height: height * scale.y,
                };

                this.drawBox(canvasRenderingContext2D, scaledBox, STYLES);
                this.drawLabel(
                    canvasRenderingContext2D,
                    label,
                    confidence,
                    scaledBox,
                    STYLES);
            });
        } catch (error) {
            console.error("Error drawing detections:", error);
        }
    },

    /**
     * Draw a bounding box
     * @param {CanvasRenderingContext2D} CanvasRenderingContext2D - Canvas context
     * @param {Object} scaledBox - Box coordinates and dimensions
     * @param {Object} styles - Styling options
     */
    drawBox(CanvasRenderingContext2D, scaledBox, styles) {
        CanvasRenderingContext2D.strokeStyle = styles.boxColor;
        CanvasRenderingContext2D.strokeRect(
            scaledBox.x,
            scaledBox.y,
            scaledBox.width,
            scaledBox.height);
    },

    /**
     * Draw label
     * @param {CanvasRenderingContext2D} CanvasRenderingContext2D - Canvas context
     * @param {string} label - Detection label
     * @param {number} confidence - Detection confidence
     * @param {Object} box - Box coordinates and dimensions
     * @param {Object} styles - Styling options
     */
    drawLabel(CanvasRenderingContext2D, label, confidence, box, styles) {
        const labelText = `${label} (${(confidence * 100).toFixed(1)}%)`;
        const textMetrics = CanvasRenderingContext2D.measureText(labelText);

        CanvasRenderingContext2D.fillStyle = `rgba(0, 0, 0, ${styles.labelBackgroundAlpha})`;
        CanvasRenderingContext2D.fillRect(
            box.x,
            box.y - styles.textHeight,
            textMetrics.width + styles.labelPadding * 2,
            styles.textHeight);

        CanvasRenderingContext2D.fillStyle = styles.textColor;
        CanvasRenderingContext2D.fillText(
            labelText,
            box.x + styles.labelPadding,
            box.y - styles.textHeight + styles.labelPadding);
    },
};
