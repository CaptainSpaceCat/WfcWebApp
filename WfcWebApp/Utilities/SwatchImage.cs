
namespace WfcWebApp.Utils
{

public class SwatchImage {
	private readonly int[,,] _accumulated; // [x, y, rgba]
	private readonly int[,] _weights;      // [x, y]
	public readonly int Width, Height;

	public SwatchImage(int width, int height) {
		Width = width;
		Height = height;
		_accumulated = new int[width, height, 4]; // R, G, B, A
		_weights = new int[width, height];
	}

	public void Clear() {
		for (int x = 0; x < Width; x++) {
			for (int y = 0; y < Height; y++) {
				for (int r = 0; r < 4; r++) {
					_accumulated[x,y,r] = 0;
				}
				_weights[x,y] = 0;
			}
		}
	}

	public void AddColorToPosition(int x, int y, Color color) {
		_accumulated[x, y, 0] += color.R;
		_accumulated[x, y, 1] += color.G;
		_accumulated[x, y, 2] += color.B;
		_accumulated[x, y, 3] += color.A;
		_weights[x, y]++;
	}

	public Color GetAverageColor(int x, int y) {
		int w = _weights[x, y];
		if (w == 0) return new Color(0, 0, 0, 0); // transparent if no color added

		return new Color(
			(byte)(_accumulated[x, y, 0] / w),
			(byte)(_accumulated[x, y, 1] / w),
            (byte)(_accumulated[x, y, 2] / w),
            (byte)(_accumulated[x, y, 3] / w)
		);
	}

	public Color[,] RenderToImage() {
		var result = new Color[Width, Height];
		for (int y = 0; y < Height; y++) {
			for (int x = 0; x < Width; x++) {
				result[x, y] = GetAverageColor(x, y);
			}
		}
		return result;
	}
}


}