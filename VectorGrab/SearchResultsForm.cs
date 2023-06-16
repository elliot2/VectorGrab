using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VectorGrab
{
    public partial class SearchResultsForm : Form
    {
        public SearchResultsForm()
        {
            InitializeComponent();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public void ShowSearchResults(List<DocumentVectorDatabase.SearchResult> searchResults)
        {
            dataGridView1.Rows.Clear();
            foreach (DocumentVectorDatabase.SearchResult searchResult in searchResults)
            {
                int index = dataGridView1.Rows.Add();
                DataGridViewRow row = dataGridView1.Rows[index];
                row.Cells[0].Value = (searchResult.Page + 1).ToString();
                row.Cells[1].Value = searchResult.Paragraph.ToString();
                string paragraphText = searchResult.ParagraphText;
                row.Cells[2].Value = paragraphText;
                row.Cells[2].Style.ForeColor = Color.DarkBlue;
                row.Cells[3].Value = searchResult.CosineSimilarity.ToString();
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
