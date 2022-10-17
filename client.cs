using System;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.IO;

public class HMAC
{
    public string type;
    public int counter;
    public string signature;
    public string consumer_id;

    public HMAC(string type, int counter, string signature, string consumer_id)
    {
        this.type = type;
        this.counter = counter;
        this.signature = signature;
        this.consumer_id = consumer_id;
    }
}

public class Client
{   
    public string host;
    private string key;
    private string secret;

    public Client(string host, string key, string secret)
    {
        this.host = host;
        this.key = key;
        this.secret = secret;
    }

    private String sha256_hash(string value)
    {
        StringBuilder Sb = new StringBuilder();
        using (var hash = SHA256.Create())            
        {
            Encoding enc = Encoding.UTF8;
            byte[] result = hash.ComputeHash(enc.GetBytes(value));
            foreach (byte b in result)
                Sb.Append(b.ToString("x2"));
        }
        return Sb.ToString();
    }

    private HMAC genenerateHMAC(string body)
    {
        string consumer_id = this.key;
		string secret = this.secret;
		int counter = 1;
		string content = body;
        if(String.IsNullOrEmpty(body)){
             content = "{}";
        }
		string prehash = String.Format("{0}{1}{2}", secret, counter.ToString(), content);
		string signature = this.sha256_hash(prehash);
        HMAC hmac = new HMAC("sha256", counter, signature, consumer_id);
		return hmac;
    }

    private string get(string route)
    {
        HMAC hmac = this.genenerateHMAC(String.Empty);
        string url = String.Format("{0}/integration{1}", this.host, route);
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.ContentType = "application/json";
        request.Headers["Consumer-ID"] = hmac.consumer_id;
        request.Headers["Counter"] = hmac.counter.ToString();
        request.Headers["Signature-Type"] = hmac.type;
        request.Headers["Signature"] = hmac.signature;
        request.UserAgent = "";
        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    private string post(string route, string body)
    {
        HMAC hmac = this.genenerateHMAC(body);
        string url = String.Format("{0}/integration{1}", this.host, route);
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Consumer-ID"] = hmac.consumer_id;
        request.Headers["Counter"] = hmac.counter.ToString();
        request.Headers["Signature-Type"] = hmac.type;
        request.Headers["Signature"] = hmac.signature;
        request.UserAgent = "";
        using (var streamWriter = new StreamWriter(request.GetRequestStream()))
        {
            streamWriter.Write(body);
        }
        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (Stream stream = response.GetResponseStream())
        using (StreamReader reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    public string devices()
    {
        string results = this.get("/v1/devices");
        return results;
    }
    
    public string enter(string device_id, string name, string device_description) {
        string url = String.Format("/v1/devices/{0}/enter", device_id);
        string body = String.Format("{{\"name\":\"{0}\",\"device_description\":\"{1}\"}}", name, device_description);
		string results = this.post(url, body);
        return results;
	}
}

public class Program
{
	public static void Main()
	{
        Client client = new Client("https://DOMAIN", "YOUR_KEY", "YOUR_SECRET");
		Console.WriteLine(client.devices());
        Console.WriteLine(client.enter("DEVICE_ID", "NAME_TO_SHOW_TO_PATIENT", "META_DATA"));
	}
}