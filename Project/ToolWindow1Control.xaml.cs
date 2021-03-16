namespace Trspo2
{
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.TextManager.Interop;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;

    using System;
    using System.IO;

    /// <summary>
    /// Interaction logic for ToolWindow1Control.
    /// </summary>
    /// 
    public class StatisticSet
    {
        public string FunctionName { get; set; }
        public string KeyWordCount { get; set; }
        public string LinesCount { get; set; }
        public string WithoutComments { get; set; }
    }
public partial class ToolWindow1Control : UserControl
    {
        private List<StatisticSet> items;
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindow1Control"/> class.
        /// </summary>
        public ToolWindow1Control()
        {
            this.InitializeComponent();
            items = new List<StatisticSet>();
            Statistic.ItemsSource = items;
            GetModules();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            items.Clear();
            string text = GetCurEditorText();
            GetModules();
           // MessageBox.Show(text);
            Statistic.Items.Refresh();

        }

        private string getFuncDeclaration(CodeElement codeElement)
        {
            CodeFunction function = codeElement as CodeFunction;
            TextPoint start = function.GetStartPoint(vsCMPart.vsCMPartHeader);
            TextPoint finish = function.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter);
            string fullSource = start.CreateEditPoint().GetText(finish);
            return fullSource;
        }
        private void GetModules()
        {
            Dispatcher.VerifyAccess();
            DTE2 dte;
            try
            {
                dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));

                ProjectItem item = dte.ActiveDocument.ProjectItem;
                FileCodeModel2 model = (FileCodeModel2)item.FileCodeModel;

                foreach (CodeElement codeElement in model.CodeElements)
                {
                    if (codeElement.Kind == vsCMElement.vsCMElementFunction)
                    {
                        //MessageBox.Show(codeElement.Kind.ToString() + ":\t" +
                        //getFuncDeclaration(codeElement) + "\n");
                        ParseFunc(getFuncDeclaration(codeElement));
                        continue;
                    }
                }

            }
            catch (Exception ex)
            {
              //  System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }

        private string GetCurEditorText()  // get text not from system, but from editor
        {
            var log = Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager2;
            IVsTextView view;

            int result = log.GetActiveView2(1, null, (uint)_VIEWFRAMETYPE.vftCodeWindow, out view);
            view.GetBuffer(out IVsTextLines ppBuffer);
            ppBuffer.GetLastLineIndex(out int lastLine, out int lastIndex);

            view.GetTextStream(0, 0, lastLine, lastIndex, out string myManagedString);
            return myManagedString;
        }

        void ParseFunc(string func_content)
        {
            int am_strings_general = 0; //общее кол-во строк
            int am_strings_without_comments = 0; //общее кол-во строк
            int keywords_am = 0; //кол-во ключевых слов
            // MessageBox.Show(func_content + "\n");
            string single_line_comm_pat = @"((//)((.*\\(\r\n))+.*)*.*)";
            string multiline_comm_pat = @"(\/\*([\s\S])*?\*\/)";
            string single_quotes_pat = @"('((\\')*|.*?(\\\r\n)*)*('|(\r\n)))";
            string double_quotes_pat = @"(""((\\"")*|.*?(\\\r\n)*)*(""|(\r\n)))";
            string empty_string = @"[\r\n]\s*[\r\n]";
            string[] temp = func_content.Split(new[] { '\n' }, StringSplitOptions.None);
            am_strings_general = temp.Length;
            string pattern = @"(" + double_quotes_pat + @"|" + single_quotes_pat + @"|" + multiline_comm_pat + @"|" + single_line_comm_pat + ")";
            Regex regex = new Regex(pattern);
            int openCurlyBracePos = func_content.IndexOf('{');
            string func_name = Regex.Replace(func_content.Substring(0, openCurlyBracePos).Trim(), pattern+'|'+ @"[\r\n]", "");
            func_name = Regex.Replace(func_name, @"\s+", " ");
            string temp2;
            string uncommented = Regex.Replace(func_content, pattern,
            match =>
            {
                if (match.Value.StartsWith("//"))
                {
                    return "/**/"+ Environment.NewLine;
                }

                if (match.Value.StartsWith("/*"))
                {
                    return "/**/";
                }
                temp2 = Regex.Replace(match.Value, @"(/\*\*/)", " ");
                return temp2;
            }, RegexOptions.Multiline);
           // MessageBox.Show(uncommented);
            using (StreamWriter outputFile = new StreamWriter(Path.Combine("WriteLines.txt"),true))
            {
                    outputFile.WriteLine(uncommented);
            }
            uncommented = Regex.Replace(uncommented, empty_string, "\r\n");
            //теперь ключевые слова, но в кавычках не считаем
            string without_com = Regex.Replace(uncommented, @"(/\*\*/)", "");
            am_strings_without_comments = Regex.Matches(uncommented, @"\r\n").Count - Regex.Matches(uncommented, @"(.*/\*\*/)").Count+1;
           // MessageBox.Show(uncommented);
            string quotes_pat = @"(" + double_quotes_pat + @"|" + single_quotes_pat + ")";
            uncommented = Regex.Replace(uncommented, quotes_pat, "");
            string keywords = @"(alignas|alignof|and|and_eq|asm|\
auto|bitand|bitor|bool|break|case|catch|char|char16_t|char32_t|class|\
compl|const|constexpr|const_cast|continue|decltype|default|delete|do|\
double|dynamic_cast|else|enum|explicit|export|extern|false|float|for|\
friend|goto|if|inline|int|long|mutable|namespace|new|noexcept|not|\
not_eq|nullptr|operator|or|or_eq|private|protected|public|register|\
reinterpret_cast|return|short|signed|sizeof|static|static_assert|\
static_cast|struct|switch|template|this|thread_local|throw|true|try\
|typedef|typeid|typename|union|unsigned|using|virtual|void|volatile|\
wchar_t|while|xor|xor_eq)";
            keywords_am = Regex.Matches(uncommented, keywords).Count;
            // MessageBox.Show(uncommented + keywords_am.ToString());
            items.Add(new StatisticSet()
            {
                FunctionName = func_name,
                KeyWordCount = keywords_am.ToString(),
                LinesCount = am_strings_general.ToString(),
                WithoutComments = am_strings_without_comments.ToString()
            });
        }
    }
}