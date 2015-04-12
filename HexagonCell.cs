﻿using MSHC.Geometry;
using MSHC.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using tessnet2;

namespace HexSolver
{
	enum HexagonType
	{
		HIDDEN,   // ORANGE
		ACTIVE,   // BLUE
		INACTIVE, // BLACK
		NOCELL,   // GRAY
		UNKNOWN   // NOT IDENTIFIED
	}

	class HexagonCell
	{
		public static readonly Color COLOR_CELL_HIDDEN = Color.FromArgb(250, 155, 95);
		public static readonly Color COLOR_CELL_ACTIVE = Color.FromArgb(100, 155, 230);
		public static readonly Color COLOR_CELL_INACTIVE = Color.FromArgb(125, 90, 125);
		public static readonly Color COLOR_CELL_NOCELL = Color.FromArgb(230, 230, 230);

		public static readonly Color[] COLOR_CELLS = new Color[] { COLOR_CELL_HIDDEN, COLOR_CELL_ACTIVE, COLOR_CELL_INACTIVE, COLOR_CELL_NOCELL };

		public static readonly int MAX_DISTANCE = 96;

		public Vec2d OCRCenter { get; private set; }
		public double OCRRadius { get; private set; }
		public Bitmap OCRImage { get; private set; }
		public Rectangle BoundingBox { get; private set; }

		public HexagonType? _Type = null;
		public HexagonType Type
		{
			get
			{
				if (_Type == null)
					_Type = GetHexagonType();

				return _Type.Value;
			}
		}

		public HexagonCell(Vec2d center, double radius, Bitmap image)
		{
			this.OCRCenter = center;
			this.OCRRadius = radius;
			this.OCRImage = image;
			this.BoundingBox = GetBoundingBox(center, radius);
		}

		private Rectangle GetBoundingBox()
		{
			return GetBoundingBox(OCRCenter, OCRRadius);
		}

		private static Rectangle GetBoundingBox(Vec2d center, double radius)
		{
			Vec2d top = new Vec2d(0, radius);
			top.Rotate(MathExt.ToRadians(30 + 3 * 60));

			Vec2d right = new Vec2d(0, radius);
			right.Rotate(MathExt.ToRadians(30 + 4 * 60));

			Vec2d bottom = new Vec2d(0, radius);
			bottom.Rotate(MathExt.ToRadians(30 + 5 * 60));

			Vec2d left = new Vec2d(0, radius);
			left.Rotate(MathExt.ToRadians(30 + 1 * 60));

			return new Rectangle((int)(center.X + left.X), (int)(center.Y + top.Y), (int)(right.X - left.X), (int)(bottom.Y - top.Y));
		}

		public Vec2d GetEdge(int edge)
		{
			Vec2d result = new Vec2d(0, OCRRadius);
			result.Rotate(MathExt.ToRadians(30 + edge * 60));

			result += OCRCenter;

			return result;
		}

		private static Color GetAverageColor(Vec2d OCRCenter, double OCRRadius, Bitmap OCRImage, Rectangle BoundingBox)
		{
			double acR = 0;
			double acG = 0;
			double acB = 0;
			double acS = 0;

			double hexheight = OCRRadius * (Math.Sin(MathExt.ToRadians(60)) / Math.Sin(MathExt.ToRadians(90)));
			for (int x = BoundingBox.Left; x < BoundingBox.Right; x++)
			{
				for (int y = BoundingBox.Top; y < BoundingBox.Bottom; y++)
				{
					double px = Math.Abs(x - OCRCenter.X);
					double py = Math.Abs(y - OCRCenter.Y);

					if (py < hexheight && py + 2 * (px - OCRRadius) < 0)
					{
						acR += OCRImage.GetPixel(x, y).R;
						acG += OCRImage.GetPixel(x, y).G;
						acB += OCRImage.GetPixel(x, y).B;
						acS += 255;
					}
				}
			}

			return Color.FromArgb((int)(255 * acR / acS), (int)(255 * acG / acS), (int)(255 * acB / acS));
		}

		private HexagonType GetHexagonType()
		{
			Color avg = GetAverageColor(OCRCenter, OCRRadius, OCRImage, BoundingBox);

			double[] distance = COLOR_CELLS.Select(p => GetColorDistance(p, avg)).ToArray();

			double min_distance = distance.Min();

			if (min_distance >= MAX_DISTANCE)
				return HexagonType.UNKNOWN;

			return (HexagonType)Enum.GetValues(typeof(HexagonType)).GetValue(distance.ToList().IndexOf(min_distance));
		}

		private static double GetColorDistance(Color a, Color b)
		{
			return Math.Sqrt(Math.Pow(a.R - b.R, 2) + Math.Pow(a.G - b.G, 2) + Math.Pow(a.B - b.B, 2));
		}

		private Bitmap GetOCRImage()
		{
			Bitmap img = OCRImage.Clone(GetBoundingBox(), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			Color refColor = COLOR_CELLS[(int)Type];

			double hexheight = OCRRadius * (Math.Sin(MathExt.ToRadians(60)) / Math.Sin(MathExt.ToRadians(90)));
			for (int x = 0; x < img.Width; x++)
			{
				for (int y = 0; y < img.Height; y++)
				{
					double px = Math.Abs(x - img.Width / 2);
					double py = Math.Abs(y - img.Height / 2);

					if (py < hexheight && py + 2 * (px - OCRRadius) < 0)
					{
						// --
					}
					else
					{
						img.SetPixel(x, y, Color.Transparent);
					}
				}
			}

			return img;
		}

		public string GetOCRString()
		{
			Bitmap img = GetOCRImage();

			Tesseract ocr = new Tesseract();
			ocr.Init(@"C:\OCRTest\tessdata", "eng", true);
			ocr.SetVariable("tessedit_char_whitelist", "0123456789-{}?");
			List<Word> result = ocr.DoOCR(img, Rectangle.Empty);

			return string.Join("", result.Select(p => p.Text));
		}
	}
}