using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;

namespace VectorGrab { 
public class DocumentVectorDatabase
{
    private readonly string connectionString;
    private readonly string tableName;

    public DocumentVectorDatabase(string databaseFilePath, string tableName)
    {
        this.connectionString = "Data Source=" + databaseFilePath;
        this.tableName = tableName;

        // Create the database file and table if they don't exist
        if (!File.Exists(databaseFilePath))
        {
            SqlCeEngine engine = new SqlCeEngine(connectionString);
            engine.CreateDatabase();

            using (SqlCeConnection connection = new SqlCeConnection(connectionString))
            {
                connection.Open();

                using (SqlCeCommand command = new SqlCeCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "CREATE TABLE " + tableName + " (documentFileName NVARCHAR(255), page BIGINT, paragraph BIGINT, vector BINARY(6144))";
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    public void Add(string documentFileName, long page, long paragraph, float[] vector)
    {
        byte[] vectorBytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, vectorBytes, 0, vectorBytes.Length);

        using (SqlCeConnection connection = new SqlCeConnection(connectionString))
        {
            connection.Open();

            using (SqlCeCommand command = new SqlCeCommand())
            {
                command.Connection = connection;
                command.CommandText = "INSERT INTO " + tableName + " (documentFileName, page, paragraph, vector) VALUES (@docName, @page, @para, @vector)";
                command.Parameters.AddWithValue("@docName", documentFileName);
                command.Parameters.AddWithValue("@page", page);
                command.Parameters.AddWithValue("@para", paragraph);
                command.Parameters.AddWithValue("@vector", vectorBytes);
                command.ExecuteNonQuery();
            }
        }
    }

    public List<float[]> GetVectors(string documentFileName, long page, long paragraph)
    {
        List<float[]> vectors = new List<float[]>();

        using (SqlCeConnection connection = new SqlCeConnection(connectionString))
        {
            connection.Open();

            using (SqlCeCommand command = new SqlCeCommand())
            {
                command.Connection = connection;
                command.CommandText = "SELECT vector FROM " + tableName + " WHERE documentFileName = @docName AND page = @page AND paragraph = @para";
                command.Parameters.AddWithValue("@docName", documentFileName);
                command.Parameters.AddWithValue("@page", page);
                command.Parameters.AddWithValue("@para", paragraph);

                using (SqlCeDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        byte[] vectorBytesFromDb = (byte[])reader["vector"];
                        float[] vectorFromDb = new float[vectorBytesFromDb.Length / sizeof(float)];
                        Buffer.BlockCopy(vectorBytesFromDb, 0, vectorFromDb, 0, vectorBytesFromDb.Length);
                        vectors.Add(vectorFromDb);
                    }
                }
            }
        }

        return vectors;
    }

    public void Clear()
    {
        using (SqlCeConnection connection = new SqlCeConnection(connectionString))
        {
            connection.Open();

            using (SqlCeCommand command = new SqlCeCommand())
            {
                command.Connection = connection;
                command.CommandText = "DELETE FROM " + tableName;
                command.ExecuteNonQuery();
            }
        }
    }

    public void Clear(string documentFileName)
    {
        using (SqlCeConnection connection = new SqlCeConnection(connectionString))
        {
            connection.Open();

            using (SqlCeCommand command = new SqlCeCommand())
            {
                command.Connection = connection;
                command.CommandText = "DELETE FROM " + tableName + " WHERE documentFileName = @docName";
                command.Parameters.AddWithValue("@docName", documentFileName);
                command.ExecuteNonQuery();
            }
        }
    }

        public class SearchResult
        {
            public long Page { get; set; }
            public long Paragraph { get; set; }

            public double CosineSimilarity { get; set; }

            public string ParagraphText { get; set; } // new property for paragraph text
        }

        public SearchResult FindMostSimilarVector(float[] vector, string documentFileName)
        {
            double maxSimilarity = double.MinValue;
            long matchingPage = -1;
            long matchingParagraph = -1;

            using (SqlCeConnection connection = new SqlCeConnection(connectionString))
            {
                connection.Open();

                using (SqlCeCommand command = new SqlCeCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT page, paragraph, vector FROM " + tableName + " WHERE documentFileName = @docName";
                    command.Parameters.AddWithValue("@docName", documentFileName);

                    using (SqlCeDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long page = reader.GetInt64(0);
                            long paragraph = reader.GetInt64(1);
                            byte[] vectorBytes = (byte[])reader["vector"];

                            float[] dbVector = new float[vectorBytes.Length / 4];
                            Buffer.BlockCopy(vectorBytes, 0, dbVector, 0, vectorBytes.Length);

                            double similarity = CalculateCosineSimilarity(vector, dbVector);
                            if (similarity > maxSimilarity)
                            {
                                maxSimilarity = similarity;
                                matchingPage = page;
                                matchingParagraph = paragraph;
                            }
                        }
                    }
                }
            }

            return new SearchResult { Page = matchingPage, Paragraph = matchingParagraph };
        }

        private double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            double dotProduct = 0.0;
            double magnitudeA = 0.0;
            double magnitudeB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += Math.Pow(vectorA[i], 2);
                magnitudeB += Math.Pow(vectorB[i], 2);
            }

            magnitudeA = Math.Sqrt(magnitudeA);
            magnitudeB = Math.Sqrt(magnitudeB);

            return dotProduct / (magnitudeA * magnitudeB);
        }


        public List<SearchResult> FindMostSimilarVectors(float[] vector, string documentFileName)
        {
            List<SearchResult> results = new List<SearchResult>();

            using (SqlCeConnection connection = new SqlCeConnection(connectionString))
            {
                connection.Open();

                using (SqlCeCommand command = new SqlCeCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT page, paragraph, vector FROM " + tableName + " WHERE documentFileName = @docName";
                    command.Parameters.AddWithValue("@docName", documentFileName);

                    using (SqlCeDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long page = reader.GetInt64(0);
                            long paragraph = reader.GetInt64(1);
                            byte[] vectorBytes = (byte[])reader["vector"];

                            float[] dbVector = new float[vectorBytes.Length / 4];
                            Buffer.BlockCopy(vectorBytes, 0, dbVector, 0, vectorBytes.Length);

                            double similarity = CalculateCosineSimilarity(vector, dbVector);

                            SearchResult result = new SearchResult
                            {
                                Page = page,
                                Paragraph = paragraph,
                                CosineSimilarity = similarity
                            };

                            results.Add(result);
                        }
                    }
                }
            }

            results.Sort((x, y) => y.CosineSimilarity.CompareTo(x.CosineSimilarity));
            return results.Take(10).ToList();
        }
    }
}