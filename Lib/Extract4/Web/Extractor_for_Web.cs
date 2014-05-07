using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Extract4.Web
{
    public class Extractor_for_Web
    {
        private readonly string baseJpUrl = "http://stocks.finance.yahoo.co.jp/stocks/detail/?code=";
        private readonly string baseUsUrl = "http://finance.yahoo.com/q?s=";
        private string html;

        private Dictionary<Source, Dictionary<Field, string>> m_nodeName = new Dictionary<Source, Dictionary<Field, string>>();
        private Dictionary<string, Dictionary<Field, string>> m_marketData = new Dictionary<string, Dictionary<Field, string>>();

        public async void Load(Source source, string bland)
        {
            if (!m_nodeName.ContainsKey(source))
                m_nodeName[source] = new Dictionary<Field, string>();

            if (!m_marketData.ContainsKey(bland))
                m_marketData[bland] = new Dictionary<Field, string>();

            string url = "";

            switch (source)
            {
                case Source.YahooJP:
                    url = baseJpUrl + bland;
                    m_nodeName[source][Field.OPEN] = "/html[1]/body[1]/div[1]/div[2]/div[2]/div[1]/div[3]/div[2]/div[2]/dl[1]/dd[1]/strong[1]";
                    m_nodeName[source][Field.HIGH] = "/html[1]/body[1]/div[1]/div[2]/div[2]/div[1]/div[3]/div[2]/div[3]/dl[1]/dd[1]/strong[1]";
                    m_nodeName[source][Field.LOW] = "/html[1]/body[1]/div[1]/div[2]/div[2]/div[1]/div[3]/div[2]/div[4]/dl[1]/dd[1]/strong[1]";
                    m_nodeName[source][Field.ASK] = "/html[1]/body[1]/div[1]/div[2]/div[2]/div[1]/div[6]/div[1]/table[1]/tr[2]/td[3]/strong[1]";
                    m_nodeName[source][Field.BID] = "/html[1]/body[1]/div[1]/div[2]/div[2]/div[1]/div[6]/div[1]/table[1]/tr[3]/td[3]/strong[1]";
                    m_nodeName[source][Field.LAST] = "/html[1]/body[1]/div[1]/div[2]/div[2]/div[1]/div[2]/div[1]/table[1]/tr[1]/td[2]";
                    break;
                case Source.YahooUS:
                    url = baseUsUrl + bland;
                    m_nodeName[source][Field.OPEN] = "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[2]/td[1]";
                    m_nodeName[source][Field.ASK] = "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[4]/td[1]/span[1]";
                    m_nodeName[source][Field.BID] = "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[3]/td[1]/span[1]";
                    m_nodeName[source][Field.ASK_SIZE] = "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[4]/td[1]/small[1]";
                    m_nodeName[source][Field.BID_SIZE] = "/html[1]/body[1]/div[4]/div[1]/div[3]/div[3]/div[1]/div[1]/table[1]/tr[3]/td[1]/small[1]";
                    break;
                default:
                    return;
            }
            
            try
            {
                await Task.Run(() => downloadData(source, url, bland));
            }
            catch (Exception err)
            {
            }
        }

        public string GetMarketData(string bland, Field field)
        {
            if (!m_marketData.ContainsKey(bland) || !m_marketData[bland].ContainsKey(field))
                return bland + " is now loading.";
            else
                return m_marketData[bland][field];
        }

        private void downloadData(Source source, string url, string bland)
        {
            WebClient wc = new WebClient();
            Stream st = wc.OpenRead(url);
            Encoding enc = Encoding.GetEncoding("utf-8");
            StreamReader sr = new StreamReader(st, enc);
            html = sr.ReadToEnd();
            sr.Close();
            st.Close();

            //HTMLを解析する
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            //XPathで取得するNodeを指定
            foreach (Field field in m_nodeName[source].Keys)
            {
                try
                {
                    doc.DocumentNode.SelectNodes(m_nodeName[source][field]);
                    var node = doc.DocumentNode.SelectSingleNode(m_nodeName[source][field]);
                    m_marketData[bland][field] = node.InnerText;
                }
                catch (Exception err)
                {
                }
            }
        }
    }
}
