
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Text;
using OpenTween.Models;
using System.Diagnostics;

namespace OpenTween
{
    public partial class TweenMain : OTBaseForm
    {
        private SpeechSynthesizer _syn;
        private LinkedList<PostClass> _speechBook = new LinkedList<PostClass>();

        void InitSpeech()
        {
            // bool b = isEnglish(" 🍫🌐 text");
            // bool c = isEnglish("‘no longer a priority; a pariah’");
            //string test = "Elon Musk admitted there was a braking issue with the Tesla's Model 3 sedan — but said it would be fixed with a firmware update in a few days. cnb.cx/2KL7YJb";
            //bool e = isEnglish(test);
            // bool f = isEnglish("An estimated 40.3 million people worldwide are victims of modern slavery. Watch @gaylelemmon, @marklagon, and E. Benjamin Skinner discuss the scourge of modern slavery and what can be done: on.cfr.org/2IDzc7Z");
            // bool g = isEnglish("‘No concessions made’ as US remains hopeful on #NorthKorea summit – Pence on.rt.com/95ta pic.twitter.com/BV3y2SoFKA");
            // bool h = isEnglish("Here'’s how to turn a Mustang GT into a performance");
            bool iii = isEnglish(@"Respect for human rights is at the ❤️ of development & peace. Without it, we can't achieve #GlobalGoals. Today & every day #StandUp4HumanRights http://standup4humanrights.org ");
            Debug.WriteLine(isJapanese(@"さっぱりと冷やしうどんは美味いなり 篤人"));
            Debug.WriteLine(isJapanese(@"昔の武士は、藩に不平があれば諫死しました。さもなければ黙って耐えました。何ものかに属する、とはそういうことです。もともと自由な人間が、何ものかに属して、美しくなるか醜くなるかの境目は、この危ない一点にしかありません。 -士道について——石原慎太郎氏への公開状-"));
            Debug.WriteLine(isJapanese(@"渡部篤 : 朝日新聞の文藝評論家  小川榮太郎氏と、飛鳥新社へのスラップ訴訟は朝日新聞の言論人への恫喝であり容認できない。言論には、言論で対応すべきである。 "));
            Debug.WriteLine(isJapanese("〜"));
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

            
            Speech();
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
        string hostSpeakable(string host)
        {
            return string.Empty;

            host = host.ToLower();
            if (false)
                ;
            else if (host.EndsWith("asahi.com"))
                host = "asahi.com";
            else if (host == "cnb.cx")
                host = "cnbc";
            else if (host.EndsWith("forbes.com"))
                host = "forbes";
            else if (host.EndsWith("nhk.or.jp"))
                host = spacer("NHK");
            else if (host.EndsWith("wsj.com"))
                host = "Wall Street Journal";
            return ". U R L : " + host;
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
                        Uri u = new Uri(node.GetAttributeValue("title", ""));
                        if (u.Host == "twitter.com")
                            node.InnerHtml = "";
                        else
                            node.InnerHtml = hostSpeakable(u.Host) + " . ";
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
        private bool isEnglish(string text)
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
        private bool isJapanese(string text)
        {
            text = commonReplace(text);

            System.Text.Encoding sjis = System.Text.Encoding.GetEncoding("sjis");
            System.Text.Encoding unicode = System.Text.Encoding.Unicode;

            byte[] sjisBytes = sjis.GetBytes(text);

            byte[] utf16Bytes = System.Text.Encoding.Convert(sjis, unicode, sjisBytes);

            string s = unicode.GetString(utf16Bytes);
            return s == text;
        }

        private void reader_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            Speech();
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
        private void Speech()
        {
            if (PauseSpeechMenuItem.Checked)
                return;

            if (_syn != null && _syn.State != SynthesizerState.Ready)
                return;

            if (_speechBook.Count == 0)
                return;

            PostClass postClass = _speechBook.Last.Value;
            string bText = processPostSpeech(postClass);
            _speechBook.RemoveLast();

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

            //if (!this.Posts.TryGetValue(this.GetStatusIdAt(index), out var post))
            //    continue;
            //TabModel foundTab;
            //int foundIndex;

                ////for (int i = 0; i < ListTab.TabPages.Count; i++)
                //{
                //    var tabPage = this.ListTab.SelectedTab;
                //    var tab = this._statuses.Tabs[tabPage.Text];
                //    foreach(PostClass pc in tab.Posts.Values)
                //    {
                //        if(pc.StateIn
                //    }
                //    var unreadIndex = tab.NextUnreadIndex;

                //    if (unreadIndex != -1)
                //    {
                //        ListTab.SelectedIndex = i;
                //        foundTab = tab;
                //        foundIndex = unreadIndex;
                //        var lst = tabPage.Tag;
                //        break;
                //    }
                //}



                int rate = 2;
            if (isEnglish(bText))
            {
                if (_enVoice != null)
                    _syn.SelectVoice(_enVoice.VoiceInfo.Name);
                else
                    _syn.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new System.Globalization.CultureInfo("en"));

                _syn.Rate = -rate;
                _syn.SpeakAsync(bText);
            }
            else if (isJapanese(bText))
            {
                _syn.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new System.Globalization.CultureInfo("ja"));
                _syn.Rate = rate;
                if (_syn.Voice.Culture.Name.ToLower().IndexOf("ja") < 0)
                {
                    _syn.SpeakAsync("No japanese voice.");
                }
                else
                {
                    _syn.SpeakAsync(bText);
                }
            }
            else
            {
                _syn.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, new System.Globalization.CultureInfo("en"));
                _syn.Rate = rate;
                _syn.SpeakAsync("Unknown language");
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
