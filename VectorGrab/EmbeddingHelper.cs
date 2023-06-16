using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using OpenAI_API;
using OpenAI_API.Embedding;

namespace VectorGrab
{
    class EmbeddingHelper
    {
        public string API_KEY;

        public EmbeddingHelper()
        {
            this.API_KEY = Registry.GetValue("HKEY_CURRENT_USER\\Software\\VectorGrab", "api-key", string.Empty) as string;
        }

        public async Task<string> complete(string query)
        {
            var api = new OpenAI_API.OpenAIAPI(API_KEY);
            api.Completions.DefaultCompletionRequestArgs.MaxTokens = 200;
            api.Completions.DefaultCompletionRequestArgs.Model = OpenAI_API.Models.Model.DefaultModel;
            return await api.Completions.GetCompletion(query);
        }

        public async Task<float[]> embed(string text)
        {
            var api = new OpenAI_API.OpenAIAPI(API_KEY);
            var result = await api.Embeddings.GetEmbeddingsAsync(text);
            return result;
        }

    }
}
