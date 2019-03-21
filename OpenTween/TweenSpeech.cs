
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Text;
using OpenTween.Models;
using System.Diagnostics;
using NTextCat;
using System.Linq;
using System.IO;

namespace OpenTween
{
    public partial class TweenMain : OTBaseForm
    {
        private SpeechSynthesizer _syn;
        private LinkedList<PostClass> _speechBook = new LinkedList<PostClass>();

        void InitSpeech()
        {
            Debug.Assert("eng" == getLanguage("This is THE OpenTween."));
            Debug.Assert("jpn" == getLanguage("これは開放ツイーンです。"));

            var s = getLanguage(@"Haruhiko Okumura : 再度Facebookで「vaccine」を検索してみる。今日はVACCINE INJURY STORIESというワクチン被害報告グループがトップに ");

            Debug.Assert(!IsHandleCreated);
            this.HandleCreated += TweenMain_HandleCreated;

            Debug.Assert(!IsDisposed);
            this.Disposed += TweenMain_Disposed;
        }

        StreamWriter logWriter_;
        private void TweenMain_HandleCreated(object sender, EventArgs e)
        {
            if (Environment.GetCommandLineArgs().Contains("--speechlog"))
            {
                string filename = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),
                    "speech.log");

                try
                {
                    // Append write
                    logWriter_ = new StreamWriter(filename, true);
                    logWriter_.WriteLine();
                    logWriter_.WriteLine(string.Format("======= {0} =======",
                        DateTime.Now.ToString()));
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void TweenMain_Disposed(object sender, EventArgs e)
        {
            if (logWriter_ != null)
            {
                logWriter_.Close();
                logWriter_.Dispose();
                logWriter_ = null;
            }
        }
        void WriteLog(string title, string detail)
        {
            if(logWriter_ != null)
            {
                try
                {
                    logWriter_.Write(title);
                    logWriter_.Write("\t");
                    logWriter_.Write(detail);
                    logWriter_.WriteLine();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

       

        bool IsSpeechEnabled
        {
            get
            {
                return EnableSpeechMenuItem.Checked;
            }
        }
        private void OnSpeechNewPost(PostClass[] notifyPosts)
        {
            //StringBuilder sbs = new StringBuilder();
            foreach (PostClass post in notifyPosts)
            {
                // sbs.Append(processPostSpeech(post));
                _speechBook.AddFirst(post);
            }
            Speak();
        }
        private string ReplaceCC(Match m)
        {
            return "";
        }
        string spacer(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach(char c in s)
            {
                sb.Append(c);
                sb.Append(' ');
            }
            return sb.ToString();
        }
        private string processPostSpeech(PostClass post)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string html = System.Web.HttpUtility.HtmlDecode(post.Text);
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);


            foreach (HtmlNode node in doc.DocumentNode.ChildNodes)
            {
                if (node.Name == "a")
                {
                    string title = node.GetAttributeValue("title", "");
                    if (!string.IsNullOrEmpty(title))
                    {
                        try
                        {
                            Uri u = new Uri(node.GetAttributeValue("title", ""));
                            if (u.Host == "twitter.com")
                                node.InnerHtml = "";
                            else
                                node.InnerHtml = " . ";
                        }
                        catch(UriFormatException)
                        { }
                    }
                    else
                    {
                        node.InnerHtml = node.InnerText;
                    }
                }
            }
            string ret = doc.DocumentNode.InnerText;
            ret = ret.Replace("#", "").Replace("@", "");
            ret = post.Nickname + " : " + ret;

            return ret;
        }

        string removeRedundant(string text, char c)
        {
            string cc = c.ToString() + c;
            string result = text;
            string pre;
            do
            {
                pre = result;
                result = pre.Replace(cc, c.ToString());
            } while (result != pre);
            return result;
        }
        string preProcessJapanese(string text)
        {
            text = removeRedundant(text, '　');
            text = text.Replace("　", "。");
           
            return text;
        }
        Regex _regRemoveEmoji = new Regex(@"\p{Cs}");

        string commonReplace(string text)
        {
            return text.Replace("…", "").
                Replace("“", "\"").
                Replace("”", "\"").
                Replace("‘", "'").
                Replace("’", "'").
                Replace("—", "-").
                Replace("–", "-").
                Replace("❤️", "").
                Replace("—", "").
                Replace("☞", "->").
                Replace(" ", " ").
                Replace("〜", "");
        }
        private bool isEnglish_obsolete(string text)
        {
            text = _regRemoveEmoji.Replace(text, "");
            text = commonReplace(text);
            System.Text.Encoding iso = System.Text.Encoding.GetEncoding("iso-8859-1");
            System.Text.Encoding unicode = System.Text.Encoding.Unicode;

            byte[] isoBytes = iso.GetBytes(text);

            byte[] utf16Bytes = System.Text.Encoding.Convert(iso, unicode, isoBytes);

            string s = unicode.GetString(utf16Bytes);
            return s == text;
        }
        private bool isJapanese_obsolete(string text)
        {
            text = commonReplace(text);

            System.Text.Encoding sjis = System.Text.Encoding.GetEncoding("sjis");
            System.Text.Encoding unicode = System.Text.Encoding.Unicode;

            byte[] sjisBytes = sjis.GetBytes(text);

            byte[] utf16Bytes = System.Text.Encoding.Convert(sjis, unicode, sjisBytes);

            string s = unicode.GetString(utf16Bytes);
            return s == text;
        }

        RankedLanguageIdentifier ncIdentifier_;
        string getLanguage(string text)
        {
            if (ncIdentifier_ == null)
            {
                var file = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),
                    @"Core14.profile.xml");
                if (!File.Exists(file))
                {
                    MessageBox.Show("Profile file of NTextCat not found.");
                    return string.Empty;
                }
                var fac = new RankedLanguageIdentifierFactory();
                ncIdentifier_ = fac.Load(file);
            }
            
            var languages = ncIdentifier_.Identify(text);
            var mostCertainLanguage = languages.FirstOrDefault();

            string lang;
            if (mostCertainLanguage == null)
                lang = string.Empty;
            else
                lang = mostCertainLanguage.Item1.Iso639_3;

            if (string.IsNullOrEmpty(lang) ||
                (lang != "eng" && lang != "jpn"))
            {
                if (isEnglish_obsolete(text))
                    lang = "eng";
                else if (isJapanese_obsolete(text))
                    lang = "jpn";
            }

            return lang;
        }

        private void reader_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            Speak();
        }
        private bool initSpeechEngine()
        {
            if (_syn == null)
            {
                _syn = new SpeechSynthesizer();
                if (_syn == null)
                {
                    MessageBox.Show("_syn init failed");
                    return false;
                }
                _syn.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(reader_SpeakCompleted);
            }
            return true;
        }
        private InstalledVoice _enVoice;
        private InstalledVoice _jpVoice;
        private void SpeakInEng(string text, int rate)
        {
            if (_enVoice != null)
                _syn.SelectVoice(_enVoice.VoiceInfo.Name);
            else
                _syn.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new System.Globalization.CultureInfo("en"));

            _syn.Rate = -rate;
            _syn.SpeakAsync(text);
        }
        private void SpeakInJpn(string text, int rate)
        {
            _syn.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new System.Globalization.CultureInfo("ja"));
            _syn.Rate = rate;
            if (_syn.Voice.Culture.Name.ToLower().IndexOf("ja") < 0)
            {
                _syn.SpeakAsync("No japanese voice installed.");
            }
            else
            {
                _syn.SpeakAsync(text);
            }
        }
        private void Speak()
        {
            if (PauseSpeechMenuItem.Checked)
                return;

            if (_syn != null && _syn.State != SynthesizerState.Ready)
                return;

            if (_speechBook.Count == 0)
                return;

            PostClass postClass = _speechBook.First.Value;
            string bText = processPostSpeech(postClass);
            _speechBook.RemoveFirst();

            if (!initSpeechEngine())
                return;

            int il = ListTab.SelectedIndex;
            int pi = postClass.StateIndex;


            for (int i = 0; i < this._statuses.Tabs[ListTab.SelectedTab.Text].Posts.Count; ++i)
            {
                this._statuses.Tabs[ListTab.SelectedTab.Text].Posts.TryGetValue(i, out var post);
                if (postClass == post)
                {
                    SelectListItem((OpenTween.OpenTweenCustomControl.DetailsListView)ListTab.SelectedTab.Tag, i);
                }
            }


            int speechRate = 2;
            var lang = getLanguage(bText);
            if (lang == "eng")
            {
                SpeakInEng(bText, speechRate);
            }
            else if (lang == "jpn")
            {
                bText = preProcessJapanese(bText);

                SpeakInJpn(bText, speechRate);
            }
            else
            {
                SpeakInEng("Could not detect language.", speechRate);
                WriteLog("Failed to detect language", bText);
            }
            Debug.WriteLine(bText);
        }

        private void SpeechAllClearMenuItem_Click(object sender, EventArgs e)
        {
            _speechBook.Clear();
            if (_syn != null)
            {
                _syn.SpeakAsyncCancelAll();
            }
        }
        private void PauseSpeechMenuItem_Click(object sender, EventArgs e)
        {
            bool newval = !PauseSpeechMenuItem.Checked;
            if (_syn != null)
            {
                if (newval)
                    _syn.Pause();
                else
                    _syn.Resume();
            }
            PauseSpeechMenuItem.Checked = newval;
        }

        private void selectEnglishEngine_click(Object sender, EventArgs e)
        {
            _enVoice = (InstalledVoice)(((ToolStripMenuItem)sender).Tag);
        }
        private void selectJapaneseEngine_click(Object sender, EventArgs e)
        {
            _jpVoice = (InstalledVoice)(((ToolStripMenuItem)sender).Tag);
        }

        private void EngineMenuItem_DropDownOpeningCommon(string lng, ToolStripMenuItem parent, EventHandler eh, InstalledVoice curV)
        {
            if (!initSpeechEngine())
                return;

            parent.DropDownItems.Clear();

            foreach (InstalledVoice iv in _syn.GetInstalledVoices())
            {
                if (iv.Enabled)
                {
                    string dn = iv.VoiceInfo.Culture.Name.ToLower();
                    if (dn.StartsWith(lng.ToLower()))
                    {
                        ToolStripMenuItem mi = new ToolStripMenuItem();
                        mi.Text = iv.VoiceInfo.Description;
                        mi.Tag = iv;
                        mi.Click += eh;
                        if (curV != null && curV.VoiceInfo.Id == iv.VoiceInfo.Id)
                            mi.Checked = true;
                        parent.DropDownItems.Add(mi);
                    }
                }
            }

            if (parent.DropDownItems.Count == 0)
            {
                ToolStripMenuItem miNone = new ToolStripMenuItem();
                miNone.Text = "<NONE>";
                miNone.Enabled = false;
                parent.DropDownItems.Add(miNone);
            }
            
        }
        private void EnglishEngineMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            EngineMenuItem_DropDownOpeningCommon("en", EnglishEngineMenuItem, selectEnglishEngine_click, _enVoice);
        }
        private void JapaneseEngineMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            EngineMenuItem_DropDownOpeningCommon("ja", JapaneseEngineMenuItem, selectJapaneseEngine_click,_jpVoice);
        }
    }
}
