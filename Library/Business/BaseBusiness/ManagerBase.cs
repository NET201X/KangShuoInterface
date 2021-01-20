using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Model.SocketModel;

namespace BaseBusiness
{
    public class ManagerBase
    {
        private SendType _sendType = SendType.Query;

        private string strHostIP = "192.168.200.3";
        private int strHostPoint = 1080;

        public byte[] socketReceiveData = null;


        public ManagerBase(SendType sendType)
        {
            _sendType = sendType;
        }

        public void SendMessage(string cookieinfo, byte[] byteSendDate, string sendHeader)
        {
            TrySendMessage(cookieinfo, byteSendDate, sendHeader, 1);
        }

        private int MaxCount = 4;

        public void TrySendMessage(string cookieinfo, byte[] byteSendDate, string sendHeader, int tryCount)
        {
            try
            {

                byte[] ByteGet = Encoding.ASCII.GetBytes(sendHeader);

                MemoryStream _MemoryStream = new MemoryStream();
                _MemoryStream.Write(ByteGet, 0, ByteGet.Length);
                _MemoryStream.Write(byteSendDate, 0, byteSendDate.Length);

                IPEndPoint EPhost = new IPEndPoint(IPAddress.Parse(strHostIP), strHostPoint);

                SocketSendReceiveMs ssrems = new SocketSendReceiveMs();

                ssrems.c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                ssrems.c.Connect(EPhost);

                if (!ssrems.c.Connected)
                {
                    throw new Exception("链接失败！");
                }

                var sendByte = _MemoryStream.ToArray();

                ssrems.c.Send(sendByte, sendByte.Length, 0);
                ssrems.ThreadEvent.WaitOne(500);

                // 接收信息
                ssrems.c.BeginReceive(ssrems.recvBytes, 0, ssrems.recvBytes.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), ssrems);

                while (socketReceiveData == null)
                {
                }
            }
            catch
            {
                if (tryCount < MaxCount)
                {
                    System.Threading.Thread.Sleep(10 * 1000);

                    tryCount++;
                    TrySendMessage(cookieinfo, byteSendDate, sendHeader, tryCount);
                }
                else
                {
                    throw new Exception("尝试3次连接不成功，请检查网络，或者重新操作");
                }
            }
        }

        //处理Http消息的回归调用函数
        void ReceiveCallBack(IAsyncResult ar)
        {
            SocketSendReceiveMs tmpSRMsg = (SocketSendReceiveMs)ar.AsyncState;
            int re = 0;

            try
            {
                re = tmpSRMsg.c.EndReceive(ar);
            }
            catch (Exception ex)
            {
                throw ex;
                //MessageBox.Show("goupi错误" + se.Message, "提示信息", MessageBoxButtons.RetryCancel, MessageBoxIcon.Information);
            }

            if (re > 0)
            {
                tmpSRMsg.Stream1.Write(tmpSRMsg.recvBytes, 0, re);
            }

            if (tmpSRMsg.Stream1.CanRead && tmpSRMsg.Stream1.Length > 0 && tmpSRMsg.HttpLength < 0)
            {
                string tmpHttpRsp = Encoding.UTF8.GetString(tmpSRMsg.Stream1.ToArray());
                if (tmpHttpRsp.IndexOf("\r\n\r\n") > 0)
                {
                    try
                    {
                        Match tmpMatch = Regex.Match(tmpHttpRsp, @"Content-length: (?'T'(\d)*)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        tmpSRMsg.tmpLen = tmpHttpRsp.IndexOf("\r\n\r\n") + 4;
                        tmpSRMsg.HttpLength = tmpSRMsg.tmpLen + Convert.ToInt32(tmpMatch.Groups[2].Value);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }

            if (tmpSRMsg.HttpLength == tmpSRMsg.Stream1.Length)
            {
                byte[] tmpBuf = tmpSRMsg.Stream1.ToArray();
                byte[] tmpData = new byte[tmpSRMsg.HttpLength - tmpSRMsg.tmpLen];

                Array.Copy(tmpBuf, tmpSRMsg.tmpLen, tmpData, 0, tmpData.Length);


                socketReceiveData = tmpData;

                /*MemoryStream mCapStream = new MemoryStream();
                mCapStream.Write(tmpData, 0, tmpData.Length);
                mCapStream.Seek(0, SeekOrigin.Begin);

                try
                {
                    _baseData.GetResponseData(mCapStream, _sendType);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                mCapStream.Close();*/
                tmpSRMsg.Stream1.Close();
                tmpSRMsg.c.Close();

                tmpSRMsg.Stream1.Dispose();
                tmpSRMsg.c.Dispose();
                tmpSRMsg.HttpLength = -1;
            }
            else
            {
                try
                {
                    tmpSRMsg.c.BeginReceive(tmpSRMsg.recvBytes, 0, tmpSRMsg.recvBytes.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), tmpSRMsg);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }

    public enum SendType
    {
        Query,
        Add
    }
}
