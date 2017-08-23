using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimpleAsepriteToSpine
{
    class AsepriteScanner
    {
        private string asepriteParentDire; //Aseprite.exe 파일 경로
        private string aseFilePath; //스캔할 .ase 파일 (이름을 제외한) 경로
        private string aseFileName; //스캔할 .ase 파일 이름

        public AsepriteScanner()
        {
            JObject jSpineObj = new JObject();
            jSpineObj.Add("skins", new JObject(new JProperty("default", new JObject())));
            string aa = jSpineObj.ToString();


            JObject jo = (JObject)jSpineObj["skins"]["default"];
            jo.Add("test", "dd");

            Console.WriteLine(aa);
        }

        public bool FindAsepriteByDialog()
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = ".exe";
            fileDialog.Filter = "Aseprite File|Aseprite.exe";

            if (fileDialog.ShowDialog() == true)
            {
                SetAsepriteParentDire(fileDialog.FileName);

                return true;
            }

            return false;
        }

        public bool FindAseFileByDialog()
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.DefaultExt = ".exe";
            fileDialog.Filter = "Aseprite (*.ase)|*.ase";

            if (fileDialog.ShowDialog() == true)
            {
                SetAseFilePath(fileDialog.FileName.Replace(fileDialog.SafeFileName, ""));
                SetAseFileName(fileDialog.SafeFileName.Replace(".ase", ""));

                return true;
            }

            return false;
        }

        public void ExportJsonWithLayersPng()
        {
            ProcessStartInfo proStartInfo = new ProcessStartInfo();
            Process process = new Process();
            proStartInfo.FileName = "cmd";
            proStartInfo.WorkingDirectory = @"C:\";
            proStartInfo.RedirectStandardOutput = true;
            proStartInfo.RedirectStandardInput = true;
            proStartInfo.RedirectStandardError = true;
            proStartInfo.UseShellExecute = false;
            proStartInfo.CreateNoWindow = true;
            process.StartInfo = proStartInfo;
            process.Start();

            //Aseprite 위치로 이동
            process.StandardInput.Write(@"cd " + asepriteParentDire + Environment.NewLine);

            //json Array 타입으로 추출, 레이어 각각을 모두 png 파일로 추출
            string writeString = "aseprite.exe -b"
                + " --split-layers " + aseFilePath + aseFileName + ".ase"
                + " --filename-format {path}\\{layer}.{extension}"
                + " --trim"
                + " --save-as "+ aseFilePath + ".png"
                + " --sheet " + aseFileName + ".png"
                + " --format json-array --data " + aseFilePath + aseFileName + "_Aseprite.json"
                + Environment.NewLine;

            process.StandardInput.Write(writeString);
            process.StandardInput.Close();

            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Close();

            //뽑힌 Aseprite json 파일을 Spine 에서 사용가능하도록 컨버팅
            ExportJsonForUseInSpine(aseFilePath + aseFileName + "_Aseprite.json");
        }

        public void ExportJsonForUseInSpine(string jsonFilePath)
        {
            string jsonString = System.IO.File.ReadAllText(jsonFilePath);

            JObject jAspObj = JObject.Parse(jsonString);
            JArray jfreams = (JArray)jAspObj["frames"];

            JObject jSpineObj = new JObject();
            jSpineObj.Add("skeleton", new JObject(new JProperty("images", aseFilePath)));
            jSpineObj.Add("bones", new JArray(new JObject(new JProperty("name", "root"))));
            jSpineObj.Add("slots", new JArray());
            jSpineObj.Add("skins", new JObject(new JProperty("default", new JObject())));

            for (int i=0; i< jfreams.Count; i++)
            {
                //※Aseprite 에서 Trim 설정으로 자른 이미지의 좌표정보는 Anchor가 왼쪽 위 기준으로 잡혀있더라.

                string filePath = (string)jfreams[i]["filename"];
                string fileName = filePath.Replace(aseFilePath, "").Replace(".ase", "");
                int imgHeight = (int)jfreams[i]["sourceSize"]["h"];
                int imgWidth = (int)jfreams[i]["sourceSize"]["w"];
                int x = (int)jfreams[i]["spriteSourceSize"]["x"];
                int y = (int)jfreams[i]["spriteSourceSize"]["y"];
                int w = (int)jfreams[i]["spriteSourceSize"]["w"];
                int h = (int)jfreams[i]["spriteSourceSize"]["h"];

                JObject jslot = new JObject();
                jslot.Add(new JProperty("name", fileName));
                jslot.Add(new JProperty("bone", "root"));
                jslot.Add(new JProperty("attachment", fileName));
                JArray jslots = (JArray)jSpineObj["slots"];
                jslots.Add(jslot);

                JObject jImgInfo = new JObject();
                jImgInfo.Add(new JProperty("x", x + w/2));
                jImgInfo.Add(new JProperty("y", imgHeight - (y + h/2)));
                jImgInfo.Add(new JProperty("width", w));
                jImgInfo.Add(new JProperty("height", h));

                JObject jskins = (JObject)jSpineObj["skins"]["default"];
                jskins.Add(fileName, new JObject(new JProperty(fileName, jImgInfo)));
            }

            jSpineObj.Add("animations", new JObject(new JProperty("animation", new JObject())));

            string spineJsonString = jSpineObj.ToString();
            System.IO.File.WriteAllText(aseFilePath + aseFileName + "_Spine.json", spineJsonString);

        }

        /*----------------
        // Getter&Setter
        -----------------*/

        public string GetAsepriteParentDire()
        {
            return asepriteParentDire;
        }
        public string GetAseFilePath()
        {
            return aseFilePath;
        }
        public string GetAseFileName()
        {
            return aseFileName;
        }

        public void SetAsepriteParentDire(string asepritePath)
        {
            asepriteParentDire = asepritePath.Replace("Aseprite.exe", "");
        }

        public void SetAseFilePath(string aseFilePath)
        {
            this.aseFilePath = aseFilePath;
        }

        public void SetAseFileName(string aseFileName)
        {
            this.aseFileName = aseFileName;
        }
    }
}
