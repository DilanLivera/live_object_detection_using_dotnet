window.CameraManager = {
    activeStreams: new Map(),

    async initializeCamera(videoElementId) {
        try {
            const videoElement = document.getElementById(videoElementId);
            if (!videoElement) return false;

            const stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    facingMode: "environment",
                    width: {ideal: 1280},
                    height: {ideal: 720},
                },
            });

            videoElement.srcObject = stream;
            this.activeStreams.set(videoElementId, stream);

            return new Promise((resolve) => {
                videoElement.onloadedmetadata = () =>
                    videoElement
                        .play()
                        .then(() => resolve(true))
                        .catch((error) => {
                            console.error("Error playing video:", error);
                            resolve(false);
                        });
            });
        } catch (error) {
            console.error("Error initializing camera:", error);
            return false;
        }
    },

    stopCamera(videoElementId) {
        const videoElement = document.getElementById(videoElementId);
        if (videoElement && videoElement.srcObject) {
            const tracks = videoElement.srcObject.getTracks();
            tracks.forEach((track) => track.stop());
            videoElement.srcObject = null;
            this.activeStreams.delete(videoElementId);
        }
    },

    async captureFrame(videoElementId) {
        try {
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

            const canvas = document.createElement("canvas");
            // Resolution set to 800x450 as higher resolutions cause processing errors
            canvas.width = 800;
            canvas.height = 450;
            const ctx = canvas.getContext("2d");

            ctx.drawImage(videoElement, 0, 0, canvas.width, canvas.height);

            const blob = await new Promise((resolve) =>
                // JPEG quality set to 0.5 (50%) as higher quality at 800x450 resolution causes processing errors
                canvas.toBlob(resolve, "image/jpeg", 0.5)
            );

            if (!blob) {
                console.error("Failed to create blob from canvas");
                return null;
            }

            const arrayBuffer = await blob.arrayBuffer();
            return new Uint8Array(arrayBuffer);
        } catch (error) {
            console.error("Error capturing frame:", error);
            return null;
        }
    },

    clearCanvas(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const ctx = canvas.getContext("2d");
        ctx.clearRect(0, 0, canvas.width, canvas.height);
    },

    drawDetections(canvasId, detections) {
        console.info("Drawing detections:", detections.length);

        try {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                console.error("Canvas element not found");
                return;
            }

            const video = document.getElementById("cameraFeed");
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;

            // The dimensions used when processing the image
            const processedWidth = 800;
            const processedHeight = 450;

            // Calculate scale factors
            const scaleX = canvas.width / processedWidth;
            const scaleY = canvas.height / processedHeight;

            console.info("Canvas dimensions:", canvas.width, "x", canvas.height);
            console.info("Scale factors:", {scaleX, scaleY});

            const ctx = canvas.getContext("2d");
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            // Set default styles
            ctx.lineWidth = 2;
            ctx.font = "16px Arial";
            ctx.textBaseline = "top";

            detections.forEach((detection, index) => {
                const {label, confidence, box} = detection;
                const {x, y, width, height} = box;

                console.info(
                    `Detection ${index}:`,
                    {
                        label,
                        confidence,
                        originalBox: {x, y, width, height},
                    });

                // Scale the coordinates from processed image size to display size
                const actualX = x * scaleX;
                const actualY = y * scaleY;
                const actualWidth = width * scaleX;
                const actualHeight = height * scaleY;

                console.info(
                    `Drawing box ${index}:`,
                    {
                        x: actualX,
                        y: actualY,
                        width: actualWidth,
                        height: actualHeight,
                        scaleX,
                        scaleY,
                    });

                // Draw bounding box
                ctx.strokeStyle = "#00ff00";
                ctx.strokeRect(actualX, actualY, actualWidth, actualHeight);

                // Draw label background
                const labelText = `${label} (${(confidence * 100).toFixed(1)}%)`;
                const textMetrics = ctx.measureText(labelText);
                const textHeight = 20;
                ctx.fillStyle = "rgba(0, 0, 0, 0.7)";
                ctx.fillRect(
                    actualX,
                    actualY - textHeight,
                    textMetrics.width + 10,
                    textHeight);

                // Draw label text
                ctx.fillStyle = "#ffffff";
                ctx.fillText(labelText, actualX + 5, actualY - textHeight + 2);
            });
        } catch (error) {
            console.error("Error drawing detections:", error);
        }
    },
};
