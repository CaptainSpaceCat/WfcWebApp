let currentColor = '#000000';
let drawing = false;
let lastPainted = null;

window.initCanvasDrawing = (canvasId, width, height, scale) => {
	const canvas = document.getElementById(canvasId);
	const ctx = canvas.getContext('2d');

	canvas.width = width;
	canvas.height = height;
	canvas.style.width = (width * scale) + "px";
	canvas.style.height = (height * scale) + "px";

    const getMousePos = (e) => {
		const rect = canvas.getBoundingClientRect();
		const x = Math.floor((e.clientX - rect.left) / scale);
		const y = Math.floor((e.clientY - rect.top) / scale);
		return { x, y };
	};

	const paint = (e) => {
		if (!drawing) return;
		const { x, y } = getMousePos(e);
		ctx.fillStyle = currentColor;

		if (lastPainted) {
			drawLine(ctx, lastPainted.x, lastPainted.y, x, y);
		} else {
			ctx.fillRect(x, y, 1, 1);
		}
		lastPainted = { x, y };
	};

    canvas.addEventListener('mousedown', (e) => {
		drawing = true;
		lastPainted = null;
		paint(e);
	});

	canvas.addEventListener('mousemove', paint);

	canvas.addEventListener('mouseup', () => {
		drawing = false;
		lastPainted = null;
	});

	canvas.addEventListener('mouseleave', () => {
		drawing = false;
		lastPainted = null;
	});
};

function drawLine(ctx, x0, y0, x1, y1) {
	const dx = Math.abs(x1 - x0);
	const dy = Math.abs(y1 - y0);
	const sx = x0 < x1 ? 1 : -1;
	const sy = y0 < y1 ? 1 : -1;
	let err = dx - dy;

	while (true) {
		ctx.fillRect(x0, y0, 1, 1);
		if (x0 === x1 && y0 === y1) break;
		const e2 = 2 * err;
		if (e2 > -dy) { err -= dy; x0 += sx; }
		if (e2 < dx) { err += dx; y0 += sy; }
	}
}

window.setDrawColor = (color) => {
    currentColor = color;
};

window.setCanvasImage = (canvasId, imageData, palette) => {
	const canvas = document.getElementById(canvasId);
	const ctx = canvas.getContext('2d');

	for (let y = 0; y < imageData.length; y++) {
		for (let x = 0; x < imageData[y].length; x++) {
			const id = imageData[y][x];
			if (id === 0) continue; // Transparent or empty
			ctx.fillStyle = palette[id]; // Assuming 1-based ID
			ctx.fillRect(x, y, 1, 1);
		}
	}
};

window.getIndexedImageFromBase64 = (base64Image) => {
	return new Promise((resolve, reject) => {
		const img = new Image();
		img.onload = () => {
			const canvas = document.createElement("canvas");
			canvas.width = img.width;
			canvas.height = img.height;
			const ctx = canvas.getContext("2d");
			ctx.drawImage(img, 0, 0);

			const imageData = ctx.getImageData(0, 0, img.width, img.height);
			const data = imageData.data;

			const colorToId = new Map();
			let nextId = 1;

			const idArray = new Int32Array(img.width * img.height);

			const pixelIdGrid = [];
			for (let y = 0; y < img.height; y++) {
				const row = [];
				for (let x = 0; x < img.width; x++) {
					const i = y * img.width + x;

					const r = data[i * 4 + 0];
					const g = data[i * 4 + 1];
					const b = data[i * 4 + 2];
					const a = data[i * 4 + 3];
					
					// Convert to hex code
					const key = `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}${a.toString(16).padStart(2, '0')}`.toUpperCase();
					if (!colorToId.has(key)) {
						colorToId.set(key, nextId++);
					}
					row.push(colorToId.get(key));
				}
				pixelIdGrid.push(row);
			}
			resolve({
				pixelIdGrid: pixelIdGrid,
				idToColorRaw: Object.fromEntries([...colorToId.entries()].map(([color, id]) => [id, color]))
			});
		};

		img.onerror = reject;
		img.src = base64Image;
	});
}