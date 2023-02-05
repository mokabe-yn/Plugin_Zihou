/*!
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * */

//プラグインのファイル名は、「Plugin_*.dll」という形式にして下さい。
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;
using FNF.Utility;
using FNF.Controls;
using FNF.XmlSerializerSetting;
using FNF.BouyomiChanApp;

namespace Plugin_Zihou {
    public class Plugin_Zihou : IPlugin {
        #region ■フィールド

        private Settings_Zihou _Settings;                                                       //設定
        private SettingFormData_Zihou _SettingFormData;
        private string _SettingFile = Base.CallAsmPath + Base.CallAsmName + ".setting"; //設定ファイルの保存場所
        private System.Threading.Timer _Timer;                                                          //タイマ
        private DateTime _NextAlartTime;                                                  //次回の時報時刻
        private ToolStripButton _Button;
        private ToolStripSeparator _Separator;

        private int _Ticks=30; // 何分おきに読み上げるか
        private string _AloudTextFile = Base.CallAsmPath + Base.CallAsmName + "_Aloud.txt"; //読み上げる文字列

        #endregion


        #region ■IPluginメンバの実装

        public string Name { get { return "時報"; } }

        public string Version { get { return "2023.02.05"; } }

        public string Caption { get { return "時刻を読み上げます。\n一時間毎の時報と、ボタンを押した際に現在時刻を読み上げます。"; } }

        public ISettingFormData SettingFormData { get { return _SettingFormData; } } //プラグインの設定画面情報（設定画面が必要なければnullを返してください）

        //プラグイン開始時処理
        public void Begin() {
            //設定ファイル読み込み
            _Settings = new Settings_Zihou(this);
            _Settings.Load(_SettingFile);
            _SettingFormData = new SettingFormData_Zihou(_Settings);

            //タイマ登録(無駄に１秒間隔＾＾；)
            _Timer = new System.Threading.Timer(Timer_Event, null, 0, 1000);

            //画面にボタンとセパレータを追加
            _Separator = new ToolStripSeparator();
            Pub.ToolStrip.Items.Add(_Separator);
            _Button = new ToolStripButton(Properties.Resources.ImgZihou);
            _Button.ToolTipText = "現在時刻を読み上げ。";
            _Button.Click += Button_Click;
            Pub.ToolStrip.Items.Add(_Button);
        }

        //プラグイン終了時処理
        public void End() {
            //設定ファイル保存
            _Settings.Save(_SettingFile);

            //タイマ開放
            if (_Timer != null) {
                _Timer.Dispose();
                _Timer = null;
            }

            //画面からボタンとセパレータを削除
            if (_Separator != null) {
                Pub.ToolStrip.Items.Remove(_Separator);
                _Separator.Dispose();
                _Separator = null;
            }
            if (_Button != null) {
                Pub.ToolStrip.Items.Remove(_Button);
                _Button.Dispose();
                _Button = null;
            }
        }

        #endregion


        #region ■メソッド・イベント処理

        //タイマイベント
        private void Timer_Event(object obj) {
            DateTime dt = DateTime.Now;
            if (dt >= _NextAlartTime) {
                //時報を登録
                AddTimeTalk(dt, true);

                //次の時報時刻をセット
                SetNextAlart();
            }
        }

        //ボタンが押されたら現在時刻を読み上げる
        private void Button_Click(object sender, EventArgs e) {
            AddTimeTalk(DateTime.Now, false);
        }

        //次の時報時刻をセットする
        internal void SetNextAlart() {
            if (_Settings.TimeSignal) {
                if (_Ticks == 0) _Ticks = 1;
                 //次の時刻をセット
                 DateTime dt = DateTime.Now;
                //_NextAlartTime = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0).AddHours(1); // 次のHourの開始時にイベントを設定
                _NextAlartTime = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute / _Ticks * _Ticks, 0).AddMinutes(_Ticks); // _Ticks分置きにイベントを発生
                //_NextAlartTime = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second / 20 * 20).AddSeconds(20); // 20秒置きにイベントを発生
                //_NextAlartTime = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second).AddSeconds(30); // 現在時刻の30秒後にイベントを設定
            } else {
                //時報無効
                _NextAlartTime = DateTime.MaxValue;
            }
        }


        /*!
         * 読み上げるテキストファイルをロード
         * @return ファイルの内容
         * */
        private string ReadAloudText(string path) {
            string ret;
            try {
                StreamReader sr = new StreamReader(path, Encoding.GetEncoding("Shift_JIS"));
                try {
                    ret = sr.ReadToEnd();
                } catch (Exception) {
                    ret = string.Format("読み上げるテキストファイルの読み込みに失敗しました。", Path.GetFileName(path));
                } finally {
                    sr.Close();
                }

            } catch (FileNotFoundException) {
                // フルパスで表記すると意図しない読み上げが発生する(PCのユーザ名が読まれる)ので対策
                ret = string.Format("読み上げるテキストファイルがありません。: {0}", Path.GetFileName(path));
            }
            return ret;
        }

        /*!@fn
         * 複数行のメッセージから該当するメッセージを選択して返す。
         * 秒とか分とかがちょうどの時なら上のほうのメッセージを読む。
         * 
         * */
        string SelectMessage(string messages, DateTime now) {
            // 秒、分、時、どこまで節目になっているかを判定
            // 秒だけは0秒ジャストで呼び出されることを期待せず3秒の猶予を与える。
            int just_level;
            if (now.Second < 3) {
                if (now.Minute == 0) {
                    if (now.Hour == 0) {
                        if (now.Day == 1) {
                            if (now.Month == 1) {
                                just_level = 5;
                            } else {
                                just_level = 4;
                            } // Month
                        } else {
                            just_level = 3;
                        } // Day
                    } else {
                        just_level = 2;
                    } // Hour
                } else { 
                    just_level = 1;
                } // Minute
            } else {  
                just_level = 0;
            } // Second

            // メッセージのどの行を読むか決定する
            string[] line_messages = messages.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            int select_line =  5 - just_level;
            if (line_messages.Length < select_line) select_line = line_messages.Length - 1;
            Console.WriteLine(messages);
            Console.WriteLine(line_messages);
            Console.WriteLine(just_level);
            Console.WriteLine(select_line);
            return line_messages[select_line];

       }
        /*!
         * 現在時刻を用いて書式化する
         * Formatでのエラーはエラーメッセージを返す(投げるではない)
         * */
        string FormatAloutText(string s) {
            var now = DateTime.Now;
            try {
                s = SelectMessage(s, now);
                return string.Format(s, now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            } catch(Exception e) {
                return e.ToString().Split('\n')[0];
            }
        }

        //時刻を読み上げる
        private void AddTimeTalk(DateTime dt, bool bJustTime) {
            StringBuilder sb = new StringBuilder();
            // 時報かアイコンクリックで処理を分ける
            // 現在古いコードにあった箇所は使用しないのでコメントアウトしている
            if (true || bJustTime) {
                string text = ReadAloudText(_AloudTextFile);
                sb.Append(FormatAloutText(text));
            } else {
                /*
                sb.Append(dt.Hour);
                sb.Append("時");
                sb.Append(dt.Minute);
                sb.Append("分");
                sb.Append(dt.Second);
                sb.Append("秒です。");
                */
            }
            Pub.AddTalkTask(sb.ToString(), -1, -1, VoiceType.Default);
        }

        #endregion


        #region ■クラス・構造体

        // 設定クラス（設定画面表示・ファイル保存を簡略化。publicなメンバだけ保存される。XmlSerializerで処理できるクラスのみ使用可。）
        public class Settings_Zihou : SettingsBase {
            //保存される情報（設定画面からも参照される）
            public bool TimeSignal = true;

            //作成元プラグイン
            internal Plugin_Zihou Plugin;

            //コンストラクタ
            public Settings_Zihou() {
            }

            //コンストラクタ
            public Settings_Zihou(Plugin_Zihou pZihou) {
                Plugin = pZihou;
            }

            //GUIなどから当オブジェクトの読み込み(設定セーブ時・設定画面表示時に呼ばれる)
            public override void ReadSettings() {

            }

            //当オブジェクトからGUIなどへの反映(設定ロード時・設定更新時に呼ばれる)
            public override void WriteSettings() {
                Plugin.SetNextAlart();
            }
        }

        // 設定画面表示用クラス（設定画面表示・ファイル保存を簡略化。publicなメンバだけ保存される。XmlSerializerで処理できるクラスのみ使用可。）
        public class SettingFormData_Zihou : ISettingFormData {
            Settings_Zihou _Setting;

            public string Title { get { return _Setting.Plugin.Name; } }
            public bool ExpandAll { get { return false; } }
            public SettingsBase Setting { get { return _Setting; } }

            public SettingFormData_Zihou(Settings_Zihou setting) {
                _Setting = setting;
                PBase = new SBase(_Setting);
            }

            //設定画面で表示されるクラス(ISettingPropertyGrid)
            public SBase PBase;
            public class SBase : ISettingPropertyGrid {
                Settings_Zihou _Setting;
                public SBase(Settings_Zihou setting) { _Setting = setting; }
                public string GetName() { return "時報設定"; }

                [Category("基本設定")]
                [DisplayName("01)時報を有効にする")]
                [Description("１時間毎に時刻を報せるかどうか。\n※タイミングは正確ではありません＾＾；")]
                public bool TimeSignal { get { return _Setting.TimeSignal; } set { _Setting.TimeSignal = value; } }

                /* ISettingPropertyGridでは設定画面での表示項目を指定できます。
                [Category   ("分類")]
                [DisplayName("表示名")]
                [Description("説明文")]
                [DefaultValue(0)]        //デフォルト値：強調表示されないだけ
                [Browsable(false)]       //PropertyGridで表示しない
                [ReadOnly(true)]         //PropertyGridで読み込み専用にする
                string  ファイル選択     →[Editor(typeof(System.Windows.Forms.Design.FolderNameEditor),       typeof(System.Drawing.Design.UITypeEditor))]
                string  フォルダ選択     →[Editor(typeof(System.Windows.Forms.Design.FileNameEditor),         typeof(System.Drawing.Design.UITypeEditor))]
                string  複数行文字列入力 →[Editor(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
                */
            }
        }
    }

    #endregion
}

