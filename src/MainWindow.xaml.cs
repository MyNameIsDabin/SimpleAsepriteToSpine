using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

namespace SimpleAsepriteToSpine
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private AsepriteScanner scanner;
        private bool isFindedAseprite = false;
        private string asepritePath = "";

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public MainWindow()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnResolveAssembly);

            scanner = new AsepriteScanner();
            InitializeComponent();
            InitINIFile();
        }

        private void Button_Click_FindAseprite(object sender, RoutedEventArgs e)
        {
            if (scanner.FindAsepriteByDialog())
            {
                tbAsepriteFilePath.Text = scanner.GetAsepriteParentDire() + "Aseprite.exe";
                SaveAsepritePathINIFile(tbAsepriteFilePath.Text);
            }
        }

        private void Button_Click_FindJson(object sender, RoutedEventArgs e)
        {
            if (isFindedAseprite)
            {
                if (scanner.FindAseFileByDialog())
                {
                    scanner.ExportJsonWithLayersPng();

                    MessageBox.Show("변환 성공");
                }
            }
            else
            {
                MessageBox.Show("Aseprite.exe 경로를 설정하세요");
            }
        }

        private void InitINIFile()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "AsepriteToSpine.ini"))
            {
                StringBuilder path = new StringBuilder();
                GetPrivateProfileString("Config", "AsepritePath", "NONE", path, 100, AppDomain.CurrentDomain.BaseDirectory + "AsepriteToSpine.ini");

                if (path.ToString() != "NONE")
                {
                    isFindedAseprite = true;
                    asepritePath = path.ToString();
                    SetAsepritePathTextBox(asepritePath);
                }
            }
        }

        private void SetAsepritePathTextBox(string asepritePath)
        {
            tbAsepriteFilePath.Text = asepritePath;
            tbAsepriteFilePath.Select(tbAsepriteFilePath.Text.Length, 0);
        }

        private void SaveAsepritePathINIFile(string asepritePath)
        {
            SetAsepritePathTextBox(asepritePath);
            WritePrivateProfileString("Config", "AsepritePath", asepritePath, AppDomain.CurrentDomain.BaseDirectory + "AsepriteToSpine.ini");
            isFindedAseprite = true;
            this.asepritePath = asepritePath;
        }

        //단일 exe 실행을 위한 함수
        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            //어셈블리가 .exe파일과 같은 폴더에 있으면 이 메서드는 호출되지 않습니다.
            Assembly thisAssembly = Assembly.GetExecutingAssembly();

            AssemblyName assemblyName = new AssemblyName(args.Name);
            string name = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";
 
            var resources = thisAssembly.GetManifestResourceNames().Where(s => s.EndsWith(name));
            if (resources.Any())
            {
                var resourceName = resources.First();
                using (Stream stream = thisAssembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        return null;
                    var block = new byte[stream.Length];
                    stream.Read(block, 0, block.Length);
                    return Assembly.Load(block);
                }
             }
            return null;
        }
    }
}
