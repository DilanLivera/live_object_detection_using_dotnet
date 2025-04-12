window.CameraManager = {
    activeStreams: new Map(),

    initializeCamera: async function (videoElementId) {
        const videoElement = document.getElementById(videoElementId);

        if (!videoElement) {
            return false;
        }

        try {
            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                return false;
            }

            const stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    width: {ideal: 1280},
                    height: {ideal: 720},
                },
            });

            videoElement.srcObject = stream;
            this.activeStreams.set(videoElementId, stream);

            return new Promise((resolve) => {
                videoElement.onloadedmetadata = () => videoElement
                    .play()
                    .then(() => resolve(true))
                    .catch((err) => resolve(false));
            });
        } catch (err) {
            console.error("Error accessing camera:", err);
            return false;
        }
    },

    stopCamera: function (videoElementId) {
        const videoElement = document.getElementById(videoElementId);
        const stream = this.activeStreams.get(videoElementId);

        if (stream) {
            stream.getTracks().forEach((track) => track.stop());
            this.activeStreams.delete(videoElementId);
        }

        if (videoElement) {
            videoElement.srcObject = null;
        }
    },
};
