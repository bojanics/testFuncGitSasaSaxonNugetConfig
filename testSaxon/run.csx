#r "XfoDotNet40Ctl65.dll"
#r "Newtonsoft.Json"

using System.Net;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using XfoDotNetCtl;
using Saxon.Api;
using System.IO;
using System.Text;
using System;

using Newtonsoft.Json;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("saxon function isprocessing a request with nuget.");

    dynamic body = req.Content.ReadAsStringAsync().Result;
    dynamic json = JsonConvert.DeserializeObject(body);
    string xml = json.xml;
    xml = WebUtility.HtmlDecode(xml);

    // Removing possible BOM chars
    int index = xml.IndexOf('<');
    if (index > 0)
    {
        xml = xml.Substring(index, xml.Length - index);
    }
    string xsl = json.xsl;
    //xsl = WebUtility.HtmlDecode(xsl);

    
    MemoryStream outFs = new MemoryStream();

    XfoObj obj = null;
   try
   {
       obj = new XfoObj();
       obj.ErrorStreamType = 2;
       obj.ExitLevel = 4;

       MemoryStream inFo = doXSLT20(xml, xsl);

       obj.Render(inFo, outFs);

       // Read stream into byte array.
       byte[] byteArray = outFs.ToArray();

       var result = req.CreateResponse();
       result.StatusCode = HttpStatusCode.OK;
       result.Content = new ByteArrayContent(byteArray);
       result.Content.Headers.Add("Content-Type", "application/pdf");

       return result;
   }
   catch (XfoException e)
   {
   log.Info("saxon function failed.");
       throw e;
   }    

    
    /*
    byte[] byteArray = outFs.ToArray();

    var result = req.CreateResponse();
    result.StatusCode = HttpStatusCode.OK;
    result.Content = new ByteArrayContent(byteArray);
    result.Content.Headers.Add("Content-Type", "text/xml");

    return result;
    */
}

static MemoryStream doXSLT20(string xml, string xsl)
{
    // Compile stylesheet
    var processor = new Processor();
    var compiler = processor.NewXsltCompiler();
    var executable = compiler.Compile(new Uri(xsl));

    // Load the source document
    byte[] byteArray = Encoding.ASCII.GetBytes(xml);
    MemoryStream xmlstream = new MemoryStream(byteArray);
    //XdmNode input = processor.NewDocumentBuilder().Build(xmlstream);


    // Do transformation to a destination
    //var destination = new DomDestination();
    var transformer = executable.Load();
    //transformer.InitialContextNode = input;
    transformer.SetInputStream(xmlstream, new Uri(xsl));

    MemoryStream inFo = new MemoryStream();
    Serializer serializer = new Serializer();
    serializer.SetOutputStream(inFo);
    //serializer.SetOutputStream(new FileStream("d:\\temp\\test.xml", FileMode.Create, FileAccess.Write));
    //serializer.SetOutputWriter(Console.Out);

    transformer.Run(serializer);


    return inFo;
}

