using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using System.Text.RegularExpressions;
using static VectorGrab.DocumentVectorDatabase;
using Microsoft.Win32;

namespace VectorGrab
{
    public partial class Form1 : Form
    {
        private string documentName;
        private DocumentVectorDatabase docVecDatabase;
        private EmbeddingHelper emb = new EmbeddingHelper();

        public Form1()
        {
            InitializeComponent();
            //databaseTools.createTable();
            docVecDatabase = new DocumentVectorDatabase("database.sdf", "table_name");
            apiKeyText.Text = Registry.GetValue("HKEY_CURRENT_USER\\Software\\VectorGrab", "api-key", string.Empty) as string;
        }


        private void AddLine(string str)
        {
             textConsole.AppendText(str + "\r\n");
        }

        private async void button1_Click(object sender, EventArgs e)
        {

            openFileDialog1.Filter = "PDF files (*.pdf)|*.pdf";
            DialogResult dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult != DialogResult.OK) return;
            // Open the PDF file
            documentName = openFileDialog1.FileName;

            if (!generateCheck.Checked) return;

            docVecDatabase.Clear(openFileDialog1.FileName);
            using (PdfDocument document = PdfDocument.Open(documentName))
            {

                long pageIndex = 0;
                foreach (var page in document.GetPages())
                {
                    var text = ContentOrderTextExtractor.GetText(page, true);
                    string[] paragraphs = Regex.Split(text, "\r\n\r\n|\r\r|\n\n");
                    // Display the paragraphs
                    long paraIndex = 0;
                    System.Diagnostics.Debug.WriteLine("PG {0}", pageIndex);
                    AddLine(String.Format("PG {0}", pageIndex));

                    foreach (string paragraph in paragraphs)
                    {
                        string[] words = paragraph.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        int wordCount = words.Length;
                        // Skip short paragraphs.
                        if (wordCount > 10) { 
                            List<float[]> vec = docVecDatabase.GetVectors(documentName, pageIndex, paraIndex);
                            if (vec.Count == 0) {
                                System.Diagnostics.Debug.WriteLine("Adding {0} {1} {2}", pageIndex, paraIndex, paragraph);
                                AddLine(String.Format("Adding {0} {1} {2}", pageIndex, paraIndex, paragraph));
                                float[] resVector = await emb.embed(paragraph);
                                docVecDatabase.Add(documentName, pageIndex, paraIndex, resVector);
                            }
                        }
                        paraIndex++;
                    }
                    pageIndex++;
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            textConsole.Clear();
            AddLine("Querying!");
            System.Diagnostics.Debug.WriteLine("*** Querying!");

            if (documentName == null || documentName == "") { return; }

            float[] res = await emb.embed(textBox1.Text);
            List<DocumentVectorDatabase.SearchResult> matches = docVecDatabase.FindMostSimilarVectors(res, documentName);
            using (PdfDocument document = PdfDocument.Open(documentName))
            {

                // actual results with text
                foreach (SearchResult match in matches)
                {
                    {
                        Page page = document.GetPage((int)match.Page + 1);
                        var text = ContentOrderTextExtractor.GetText(page, true);
                        string[] paragraphs = Regex.Split(text, "\r\n\r\n|\r\r|\n\n");

 
                        match.ParagraphText = paragraphs[match.Paragraph];
                        match.ParagraphText = match.ParagraphText.Replace("\n", Environment.NewLine);

                        AddLine(String.Format("PG {0} {1} {2}", (int)match.Page + 1, match.Paragraph, paragraphs[match.Paragraph]));
                        AddLine("");
                        AddLine("");
                        Console.WriteLine("PG {0} {1} {2}", (int)match.Page + 1, match.Paragraph, paragraphs[match.Paragraph]);
                    }
                }

                // test


                if (pageCompCheck.Checked)
                {
                    string completion = "";
                    for (int i = 0; i < 5; i+=2)
                    {
                        string req = "The following is a Question followed by a Response.  If the information cannot be found, response is 'No context':\r\n" +
                        "\r\n" +
                        "\r\n" +
                        "Question:\r\n" +
                        textBox1.Text +
                        "\r\n";
                        for (int j = i; j < i+2; j++) {
                            Page page = document.GetPage((int)matches[j].Page + 1);
                            var text = ContentOrderTextExtractor.GetText(page, true);
                            AddLine(String.Format("Completion {0}", (int)matches[j].Page + 1));
                            req = req + "Here is page " + (int)matches[j].Page + 1 + " of information to refer to for the answer :\r\n" +
                            "\r\n" +
                            text +
                            "\r\n";
                        }
                        req = req + "\r\n" +
                        "Response:\r\n" +
                        "\r\n";

                        try
                        {
                            completion = await emb.complete(req);
                        } catch
                        {
                            System.Diagnostics.Debug.WriteLine("*** Too much!");
                        }
                        if (completion.Contains("No context") == false)
                        {
                            break;
                        }
                    }
                    ResponseForm responseForm = new ResponseForm();
                    responseForm.showCompletion(completion);
                    responseForm.ShowDialog();


                }
                else
                {
                    SearchResultsForm resultsForm = new SearchResultsForm();
                    resultsForm.ShowSearchResults(matches);
                    resultsForm.ShowDialog();
                }
            }
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if (textBox1.Text == "Enter your text here...")
            {
                textBox1.Text = "";
                textBox1.ForeColor = Color.Black;
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                textBox1.Text = "Enter your text here...";
                textBox1.ForeColor = Color.Gray;
            }
        }

        private void save_Click(object sender, EventArgs e)
        {
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\VectorGrab", "api-key", apiKeyText.Text, RegistryValueKind.String);
            emb.API_KEY = apiKeyText.Text;
        }

        private void apiKeyText_TextChanged(object sender, EventArgs e)
        {
            emb.API_KEY = apiKeyText.Text;
        }
    }
}
