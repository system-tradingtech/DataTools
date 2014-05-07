using System;
using System.Collections.Generic;

using System.Text;
using System.Threading;
using System.Windows.Forms;

using ExcelDna.Integration.Rtd;

using Extract4;
using Extract4.Web;

public class MarketDataDNA_RealTimeServer : IRtdServer
{
    private IRTDUpdateEvent m_callback;
    private System.Windows.Forms.Timer m_timer;
    private const int INTERVAL = 5000;

    private Dictionary<int, string> m_source;
    private Dictionary<int, string> m_bland;
    private Dictionary<int, string> m_field;
    private Dictionary<int, string> m_data;

    private Extractor_for_Web m_extractor = new Extractor_for_Web();

    #region IRtdServer Members
    public object ConnectData(int topicId, ref Array Strings, ref bool GetNewValues)
    {
        m_source[topicId] = Strings.GetValue(0).ToString();
        m_bland[topicId] = Strings.GetValue(1).ToString();
        m_field[topicId] = Strings.GetValue(2).ToString();

        m_timer.Start();

        return "Data not found.";
    }

    public void DisconnectData(int topicId)
    {
        if (m_source.ContainsKey(topicId))
            m_source.Remove(topicId);
        if (m_bland.ContainsKey(topicId))
            m_bland.Remove(topicId);
        if (m_field.ContainsKey(topicId))
            m_field.Remove(topicId);
        if (m_data.ContainsKey(topicId))
            m_data.Remove(topicId);
    }

    public int Heartbeat()
    {
        return 1;
    }

    public Array RefreshData(ref int topicCount)
    {
        object[,] results = new object[2, m_data.Count];

        int index = 0;

        foreach (int topicId in m_data.Keys)
        {
            results[0, index] = topicId;
            results[1, index] = m_data[topicId];

            ++index;
        }

        topicCount = m_data.Count;

        return results;
    }

    public int ServerStart(IRTDUpdateEvent CallbackObject)
    {
        m_source = new Dictionary<int, string>();
        m_bland = new Dictionary<int, string>();
        m_field = new Dictionary<int, string>();
        m_data = new Dictionary<int, string>();

        m_callback = CallbackObject;

        m_timer = new System.Windows.Forms.Timer();
        m_timer.Tick += Callback;
        m_timer.Interval = INTERVAL;

        return 1;
    }

    public void ServerTerminate()
    {
        if (m_timer != null)
        {
            m_timer.Tick -= Callback;
            m_timer.Stop();
            m_timer.Dispose();
            m_timer = null;
        }
    }

    private string GetTime()
    {
        return DateTime.Now.ToString("HH:mm:ss.fff");
    }

    private void Callback(object sender, EventArgs e)
    {
        //
        foreach (int topicId in m_bland.Keys)
        {
            Source source;
            Field field;

            if (!Enum.TryParse<Source>(m_source[topicId], out source))
                continue;

            if (!Enum.TryParse<Field>(m_field[topicId], out field))
                continue;

            m_extractor.Load(source, m_bland[topicId]);

            m_data[topicId] = m_extractor.GetMarketData(m_bland[topicId], field);
        }
        m_callback.UpdateNotify();
    }
    #endregion
}