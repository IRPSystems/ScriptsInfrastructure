
using Services.Services;
using Syncfusion.DocIO.DLS;
using Syncfusion.UI.Xaml.Diagram;
using System;
using System.IO;
using System.Windows.Input;
using Syncfusion.DocIO;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Drawing;
using System.Diagnostics;

namespace ScriptHandler.Services
{
	public class ExportDiagramService
	{
		public void Export(
			string scriptPath,
			string scriptName,
			SfDiagram diagram)
		{
			Mouse.OverrideCursor = Cursors.Wait;

			try
			{
				ExportToWord(
					scriptPath,
					scriptName,
					diagram);

				ExportToImage(
					scriptPath,
					diagram);
			}
			catch (Exception ex) 
			{
				LoggerService.Error(this, "Failed to export the diagram", "Export error", ex);
			}

			


			Mouse.OverrideCursor = null;
		}

		public void ExportToWord(
			string scriptPath,
			string scriptName,
			SfDiagram diagram)
		{
			string exportPathDocx = scriptPath.Replace(".scr", ".docx");
			exportPathDocx = exportPathDocx.Replace(".tst", ".docx");

			try
			{
				WordDocument documentTry = new WordDocument(exportPathDocx);
			}
			catch(Exception ex) 
			{ 
				if(ex is IOException && ex.Message.Contains("The process cannot access the file"))
				{
					LoggerService.Error(this, "The file is already open", "Export error");
					return;
				}
			}

			diagram.ExportSettings.ExportStream = new MemoryStream();
			diagram.Export();

			BitmapImage imageHeader = new BitmapImage();
			imageHeader.BeginInit();
			imageHeader.StreamSource = Application.GetResourceStream(new
				Uri("pack://application:,,,/Evva;component/Resources/ExportHeader.png", UriKind.Absolute)).Stream;
			imageHeader.EndInit();

			BitmapImage imageFooter = new BitmapImage();
			imageFooter.BeginInit();
			imageFooter.StreamSource = Application.GetResourceStream(new
				Uri("pack://application:,,,/Evva;component/Resources/ExportFooter.png", UriKind.Absolute)).Stream;
			imageFooter.EndInit();

			BitmapImage imageWarning = new BitmapImage();
			imageWarning.BeginInit();
			imageWarning.StreamSource = Application.GetResourceStream(new
				Uri("pack://application:,,,/Evva;component/Resources/ExportWarnings.png", UriKind.Absolute)).Stream;
			imageWarning.EndInit();



			WordDocument document = new WordDocument();
			//Add a section & a paragraph in the empty document
			IWSection section = document.AddSection();

			section.PageSetup.PageStartingNumber = 1;
			section.PageSetup.RestartPageNumbering = true;
			section.PageSetup.PageNumberStyle = PageNumberStyle.Arabic;

			IWParagraph headerParagraph = section.HeadersFooters.OddHeader.AddParagraph();
			headerParagraph.ParagraphFormat.HorizontalAlignment = Syncfusion.DocIO.DLS.HorizontalAlignment.Left;
			//Sets after spacing for paragraph.
			headerParagraph.ParagraphFormat.AfterSpacing = 8;
			//Adds a picture into the paragraph
			headerParagraph.AppendPicture(Image.FromStream(imageHeader.StreamSource));


			IWParagraph footerParagraph = section.HeadersFooters.Footer.AddParagraph();
			footerParagraph.ParagraphFormat.HorizontalAlignment = Syncfusion.DocIO.DLS.HorizontalAlignment.Center;
			//Adds page number field to the document
			footerParagraph.AppendField("Page", FieldType.FieldPage);
			footerParagraph.AppendText("\t");
			//Adds a picture into the paragraph
			footerParagraph.AppendPicture(Image.FromStream(imageFooter.StreamSource));


			//Adds a new simple paragraph into the section
			IWParagraph firstParagraph = section.AddParagraph();
			//Sets the paragraph's horizontal alignment as justify
			firstParagraph.ParagraphFormat.HorizontalAlignment = Syncfusion.DocIO.DLS.HorizontalAlignment.Center;
			//Adds a text range into the paragraph
			IWTextRange firstTextRange = firstParagraph.AppendText(scriptName);
			//sets the font formatting of the text range
			firstTextRange.CharacterFormat.Bold = true;
			firstTextRange.CharacterFormat.FontName = "Arial";
			firstTextRange.CharacterFormat.FontSize = 30;
			//Adds another text range into the paragraph

			IWParagraph secondParagraph = section.AddParagraph();
			//Sets the paragraph's horizontal alignment as justify
			secondParagraph.ParagraphFormat.HorizontalAlignment = Syncfusion.DocIO.DLS.HorizontalAlignment.Center;
			IWTextRange secondTextRange = secondParagraph.AppendText(DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss"));
			//Sets after spacing for paragraph.
			secondParagraph.ParagraphFormat.AfterSpacing = 50;
			//sets the font formatting of the text range
			secondTextRange.CharacterFormat.Bold = true;
			secondTextRange.CharacterFormat.FontName = "Arial";
			secondTextRange.CharacterFormat.FontSize = 15;

			IWParagraph warningParagraph = section.AddParagraph();
			warningParagraph.ParagraphFormat.HorizontalAlignment = Syncfusion.DocIO.DLS.HorizontalAlignment.Center;
			//Sets after spacing for paragraph.
			warningParagraph.ParagraphFormat.AfterSpacing = 8;
			//Adds a picture into the paragraph
			warningParagraph.AppendPicture(Image.FromStream(imageWarning.StreamSource));



			IWParagraph paragraph = section.AddParagraph();
			paragraph.ParagraphFormat.HorizontalAlignment = Syncfusion.DocIO.DLS.HorizontalAlignment.Center;
			//Sets after spacing for paragraph.
			paragraph.ParagraphFormat.AfterSpacing = 8;
			//Adds a picture into the paragraph
			AddDiagramImage(
				diagram,
				section,
				paragraph);

			//Save and close the Word document
			document.Save(exportPathDocx);
			document.Close();

			Process.Start("explorer.exe", Path.GetDirectoryName(exportPathDocx));
		}

		private void AddDiagramImage(
			SfDiagram diagram,
			IWSection section,
			IWParagraph paragraph)
		{
			IWPicture picture = paragraph.AppendPicture(Image.FromStream(diagram.ExportSettings.ExportStream)); 
			
			float clientWidth = section.PageSetup.ClientWidth;
			float clientHeight = section.PageSetup.PageSize.Height - section.PageSetup.Margins.Top - section.PageSetup.Margins.Bottom;
			float scalePer = 100;
			if (picture.Width > clientWidth)
			{
				scalePer = clientWidth / picture.Image.Width * 100;
			}
			else if (picture.Height > clientHeight)
			{
				scalePer = clientHeight / picture.Image.Height * 100;
			}
			
			picture.WidthScale = scalePer;
			picture.HeightScale = scalePer;

			
		}

		private void ExportToImage(
			string scriptPath,
			SfDiagram diagram)
		{
			string exportPathImage = scriptPath.Replace(".scr", ".png");
			exportPathImage = exportPathImage.Replace(".tst", ".png");

			ExportSettings settings = new ExportSettings()
			{
				FileName = exportPathImage,
			};

			diagram.ExportSettings = settings;
			diagram.Export();
		}
	}
}
