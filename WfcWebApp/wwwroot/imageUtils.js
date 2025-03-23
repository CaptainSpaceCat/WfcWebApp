window.getImagePixels = async (base64String) => {
    return new Promise((resolve, reject) => {
        let img = new Image();
        img.onload = function () {
            let canvas = document.createElement("canvas");
            
            canvas.width = img.width;
            canvas.height = img.height;
            
            let ctx = canvas.getContext("2d", { willReadFrequently: true });
            ctx.drawImage(img, 0, 0);
            
            let imageData = ctx.getImageData(0, 0, img.width, img.height).data;
            
            resolve({ width: img.width, height: img.height, pixeldata: Array.from(imageData) });
        };
        img.onerror = reject;
        img.src = base64String;
    });
};


window.updateCanvasPixels = (canvasId, width, height, pixeldata) => {
    let canvas = document.getElementById(canvasId);
    if (!canvas) return;

    let ctx = canvas.getContext("2d");
    let canvasData = ctx.getImageData(0, 0, canvas.width, canvas.height);

    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            var index_in = (y * width + x) * 4;
            var index_out = (y * canvas.width + x) * 4;
            for (let i = 0; i < 4; i++) {
                canvasData.data[index_out + i] = pixeldata[index_in + i]
            }
        }
    }

    ctx.putImageData(canvasData, 0, 0);
};