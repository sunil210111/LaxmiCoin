using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using FindAndReplace.App;

namespace NumberedTextBox
{
	public partial class NumberedTextBoxUC : UserControl
	{

		public NumberedTextBoxUC()
		{
			InitializeComponent();

			numberLabel.Font = new Font(richTextBox1.Font.FontFamily, 8);
		}


		public void updateNumberLabel()
		{
			//we get index of first visible char and number of first visible line
			Point pos = new Point(0, 0);
			int firstIndex = richTextBox1.GetCharIndexFromPosition(pos);
			int firstLine = richTextBox1.GetLineFromCharIndex(firstIndex);

			//now we get index of last visible char and number of last visible line
			pos.X = ClientRectangle.Width;
			pos.Y = ClientRectangle.Height;
			int lastIndex = richTextBox1.GetCharIndexFromPosition(pos);
			int lastLine = richTextBox1.GetLineFromCharIndex(lastIndex);

			//this is point position of last visible char, we'll use its Y value for calculating numberLabel size
			pos = richTextBox1.GetPositionFromCharIndex(lastIndex);

			if (MainForm.RichTextBoxLinNumbers != null)
			{
				var lineNumbers = MainForm.RichTextBoxLinNumbers;

				//finally, renumber label
				numberLabel.Text = "";
				string format = "D" + MainForm.LineNumbersDigitCount;

				var highLightFont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
				var regularFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
				numberLabel.Font = regularFont;

				for (int i = firstLine; i <= lastLine ; i++)
				{
					int lineNumber;

					var lineNumberStr = lineNumbers[i];
					if (lineNumberStr!="...")
					{
						var splitLineNumber = lineNumberStr.Split('-');

						if (Int32.TryParse(splitLineNumber[0], out lineNumber))
						{
							numberLabel.Text += lineNumber.ToString(format) + "\n";
						}

					} 
					else numberLabel.Text += lineNumbers[i] + "\n";
				}

				//highliting

				int j = 0;
				for (int i = firstLine; i <= lastLine; i++)
				{
					int lineNumber;

					var lineNumberStr = lineNumbers[i];
					if (lineNumberStr != "...")
					{
						var splitLineNumber = lineNumberStr.Split('-');

						if (Int32.TryParse(splitLineNumber[0], out lineNumber))
						{
							if (splitLineNumber[1] == "True")
							{

								var selectionStart = numberLabel.GetFirstCharIndexFromLine(j);

								numberLabel.Select(selectionStart, MainForm.LineNumbersDigitCount);

								numberLabel.SelectionFont = highLightFont;

								numberLabel.SelectionColor = Color.Black;
							}
						}
					}
					j++;
				}
				
			}

		}


		private void richTextBox1_TextChanged(object sender, EventArgs e)
		{
			//updateNumberLabel();
		}

		private void richTextBox1_VScroll(object sender, EventArgs e)
		{
			//move location of numberLabel for amount of pixels caused by scrollbar
			//int d = richTextBox1.GetPositionFromCharIndex(0).Y % (richTextBox1.Font.Height + 1);
			//numberLabel.Location = new Point(0, d);

			//updateNumberLabel();

			//numberLabel.VScroll()
		}

		private void richTextBox1_Resize(object sender, EventArgs e)
		{
			richTextBox1_VScroll(null, null);
		}

		private void richTextBox1_FontChanged(object sender, EventArgs e)
		{
			//updateNumberLabel();
			//richTextBox1_VScroll(null, null);
		}
	}
}
