let currentColor = '#000000';
let cellSize = 8;
const scale = 512 / 64; //pixel size of canvas / logical size of canvas

window.initCanvasDrawing = (canvasId, dotNetHelper) => {
    const canvas = document.getElementById(canvasId);
    const ctx = canvas.getContext('2d');

    canvas.addEventListener('mousedown', e => {
        const rect = canvas.getBoundingClientRect();
        const x = Math.floor((e.clientX - rect.left) / cellSize) * cellSize / scale;
        const y = Math.floor((e.clientY - rect.top) / cellSize) * cellSize / scale;
        ctx.fillStyle = currentColor;
        ctx.fillRect(x, y, cellSize/scale, cellSize/scale);

        dotNetHelper.invokeMethodAsync('OnCanvasDraw', x / cellSize, y / cellSize);
    });
};

window.setDrawColor = (color) => {
    currentColor = color;
};
