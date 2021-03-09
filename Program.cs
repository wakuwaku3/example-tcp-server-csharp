using System.Threading.Tasks;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ExampleTCPClient
{
  class Program
  {
    static void Main(string[] args)
    {
      var port = 2001;

      //TcpListenerオブジェクトを作成する
      var listener = new TcpListener(IPAddress.IPv6Any, port);
      listener.Server.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IPv6, System.Net.Sockets.SocketOptionName.IPv6Only, 0);

      try
      {
        //Listenを開始する
        listener.Start();

        while (true)
        {
          var client = listener.AcceptTcpClient();
          Task.Run(() =>
          {
            using (client)
            using (var ns = client.GetStream())
            {
              while (true)
              {
                var enc = Encoding.UTF8;
                var disconnected = false;
                var resMsg = "";
                using (var ms = new MemoryStream())
                {
                  var resBytes = new byte[256];
                  var resSize = 0;
                  do
                  {
                    //データの一部を受信する
                    resSize = ns.Read(resBytes, 0, resBytes.Length);
                    //Readが0を返した時はクライアントが切断したと判断
                    if (resSize == 0)
                    {
                      disconnected = true;
                      break;
                    }
                    //受信したデータを蓄積する
                    ms.Write(resBytes, 0, resSize);
                    //まだ読み取れるデータがあるか、データの最後が\nでない時は、
                    // 受信を続ける
                  } while (ns.DataAvailable || resBytes[resSize - 1] != '\n');
                  //受信したデータを文字列に変換
                  resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                  //末尾の\nを削除
                  resMsg = resMsg.TrimEnd('\n');
                  Console.WriteLine(resMsg);
                }
                if (!disconnected)
                {
                  //クライアントにデータを送信する
                  //クライアントに送信する文字列を作成
                  string sendMsg = resMsg.Length.ToString();
                  //文字列をByte型配列に変換
                  byte[] sendBytes = enc.GetBytes(sendMsg + '\n');
                  //データを送信する
                  ns.Write(sendBytes, 0, sendBytes.Length);
                  Console.WriteLine(sendMsg);
                }
                else
                {
                  Console.WriteLine("クライアントが切断されました");
                  break;
                }
              }
            }
          });
        }
      }
      finally
      {
        listener.Stop();
      }
    }
  }
}
