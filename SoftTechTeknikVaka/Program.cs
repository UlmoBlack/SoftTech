using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftTechTeknikVaka
{
    class Program
    {
        static List<ConcurrentQueue<string>> SentencesQueue = new List<ConcurrentQueue<string>>();
        static ConcurrentDictionary<string, int> WordDetails = new ConcurrentDictionary<string, int>();

        static string TextPath = GetConfigParams("textPath");
        static string OutTextPath = GetConfigParams("outTextPath") + "SampleResult_" + System.DateTime.Now.ToString("yyMMddhhmmss") + ".txt";
        static void Main(string[] args)
        {


            MainThread().Wait();

            //Console.ReadLine();

        }


        static async Task MainThread()
        {


            

            // Cümleleri ayırdık
            List<string> SentencesFile = new List<string>();
            SentencesFile = GetSentences();
            ///

            //// Thread ve queue sayısını ayarladık
            int ThreadCount = Convert.ToInt16(GetConfigParams("threadCount"));

            /*if (SentencesFile.Count < ThreadCount)
               ThreadCount = SentencesFile.Count;*/

            Task[] WorkerTasks = new Task[ThreadCount];

            for (int i = 0; i < ThreadCount; i++)
            {
                SentencesQueue.Add(new ConcurrentQueue<string>());
            }
            ////


            // Cümleleri queuelara yerleştirdik (. . gibi durumları kelime olarak görmemesi için sayılarını burada tuttuk.)
            int SentenceCount = 0;
            int[] ThreadCounter = new int[ThreadCount];

            for (int i = 0; i < SentencesFile.Count; i++)
            {
                if (!String.IsNullOrEmpty(SentencesFile[i]))
                {
                    SentencesQueue[i % ThreadCount].Enqueue(SentencesFile[i]);
                    SentenceCount++;
                    ThreadCounter[i % ThreadCount]++;
                }
            }

            //


            // tasklara idlerini de methodlara göndererek taskları başlattık
            for (int i = 0; i < ThreadCount; i++)
            {
                WorkerTasks[i] = HelperThread(i);
            }


            await Task.WhenAll(WorkerTasks);

            //


            //// Thread Safety için kullandığımız Dictionary'i sıralamak için List'e çevirdik
            List<Words> WordsResults = new List<Words>();
            foreach (var item in WordDetails)
            {
                WordsResults.Add(new Words(item.Key, item.Value));
            }

            // istenilen hesaplamalar
            var SortedWords = WordsResults.OrderByDescending(a => a.WordCount).ToList(); // sıralama
            var WordsCount = SortedWords.Sum(item => item.WordCount); // kelime adetleri
            var Avg = WordsCount / SentenceCount; // ortalama

            writeResult(SortedWords, WordsCount, Avg, ThreadCounter, SentenceCount);
        }

        private static void writeResult(List<Words> SortedWords, int WordsCount, int Avg, int[] ThreadCounter, int SentenceCount)
        {

            File.AppendAllText(OutTextPath, "Sentence Count: " + SentenceCount + Environment.NewLine);
            File.AppendAllText(OutTextPath, "Avg. Word Count : " + Avg.ToString() + Environment.NewLine
                + "Thread counts : " + Environment.NewLine);
            for (int i = 0; i < ThreadCounter.Length; i++)
            {
                File.AppendAllText(OutTextPath, "\tThreadId =" + i + " Count =" + ThreadCounter[i] + Environment.NewLine);
            }

            foreach (var item in SortedWords)
            {
                File.AppendAllText(OutTextPath, item.WordValue + " " + item.WordCount + Environment.NewLine);
            }


        }





        static async Task HelperThread(int ThreadId)
        {

            string CurrentSentence;
            await Task.Delay(1);

            while (SentencesQueue[ThreadId].Count > 0)
            {
                SentencesQueue[ThreadId].TryDequeue(out  CurrentSentence);
                if (!string.IsNullOrEmpty(CurrentSentence))
                {
                    string[] Kelimeler = CurrentSentence.Split(' ');

                    foreach (var kelime in Kelimeler)
                    {
                        if (!string.IsNullOrEmpty(kelime))
                        {


                            ConsolideWords(kelime, ThreadId);


                        }
                    }
                }
            }


        }

        private static void ConsolideWords(string word, int ThreadId)
        {
            bool Success = false;
            if (WordDetails == null)
            {
                do
                {
                    Success = WordDetails.TryAdd(word, 1);
                } while (!Success);

            }
            else if (!WordDetails.ContainsKey(word))
            {
                do
                {
                    Success = WordDetails.TryAdd(word, 1);
                } while (!Success);

            }
            else
            {
                do
                {
                    Success = WordDetails.TryUpdate(word, WordDetails[word] + 1, WordDetails[word]);
                } while (!Success);

            }


        }




        public static List<string> GetSentences()
        {


            if (File.Exists(TextPath))
            {
                string readText = File.ReadAllText(TextPath);
                char[] EndOfSentenceChars = { '.', '!', '?', ':', '\n', '\0' };
                string[] SentencesFile = readText.Split(EndOfSentenceChars);

                return new List<string>(SentencesFile);

            }

            return null;

        }

        public static string GetConfigParams(string ParamName)
        {
            return System.Configuration.ConfigurationManager.AppSettings.Get(ParamName);
        }

        public class Words
        {

            public Words(string wordValue, int wordCount)
            {
                WordValue = wordValue;
                WordCount = wordCount;

            }

            public int WordCount { get; set; }
            public string WordValue { get; set; }

        }



    }
}
