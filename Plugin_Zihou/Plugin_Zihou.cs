//�v���O�C���̃t�@�C�����́A�uPlugin_*.dll�v�Ƃ����`���ɂ��ĉ������B
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
        #region ���t�B�[���h

        private Settings_Zihou         _Settings;                                                       //�ݒ�
        private SettingFormData_Zihou  _SettingFormData;
        private string                 _SettingFile = Base.CallAsmPath + Base.CallAsmName + ".setting"; //�ݒ�t�@�C���̕ۑ��ꏊ
        private System.Threading.Timer _Timer;                                                          //�^�C�}
        private DateTime               _NextAlartTime;                                                  //����̎��񎞍�
        private ToolStripButton        _Button;
        private ToolStripSeparator     _Separator;

        #endregion


        #region ��IPlugin�����o�̎���

        public string           Name            { get { return "�����ǂݏグ"; } }

        public string           Version         { get { return "2009/07/22��"; } }

        public string           Caption         { get { return "������ǂݏグ�܂��B\n�ꎞ�Ԗ��̎���ƁA�{�^�����������ۂɌ��ݎ�����ǂݏグ�܂��B"; } } 

        public ISettingFormData SettingFormData { get { return _SettingFormData; } } //�v���O�C���̐ݒ��ʏ��i�ݒ��ʂ��K�v�Ȃ����null��Ԃ��Ă��������j

        //�v���O�C���J�n������
        public void Begin() {
            //�ݒ�t�@�C���ǂݍ���
            _Settings = new Settings_Zihou(this);
            _Settings.Load(_SettingFile);
            _SettingFormData = new SettingFormData_Zihou(_Settings);

            //�^�C�}�o�^(���ʂɂP�b�Ԋu�O�O�G)
            _Timer = new System.Threading.Timer(Timer_Event, null, 0, 1000);

            //��ʂɃ{�^���ƃZ�p���[�^��ǉ�
            _Separator = new ToolStripSeparator();
            Pub.ToolStrip.Items.Add(_Separator);
            _Button = new ToolStripButton(Properties.Resources.ImgZihou);
            _Button.ToolTipText = "���ݎ�����ǂݏグ�B";
            _Button.Click      += Button_Click;
            Pub.ToolStrip.Items.Add(_Button);
        }

        //�v���O�C���I��������
        public void End() {
            //�ݒ�t�@�C���ۑ�
            _Settings.Save(_SettingFile);

            //�^�C�}�J��
            if (_Timer != null) {
                _Timer.Dispose();
                _Timer = null;
            }

            //��ʂ���{�^���ƃZ�p���[�^���폜
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


        #region �����\�b�h�E�C�x���g����

        //�^�C�}�C�x���g
        private void Timer_Event(object obj) {
            DateTime dt = DateTime.Now;
            if (dt >= _NextAlartTime) {
                //�����o�^
                AddTimeTalk(dt, true);

                //���̎��񎞍����Z�b�g
                SetNextAlart();
            }
        }

        //�{�^���������ꂽ�猻�ݎ�����ǂݏグ��
        private void Button_Click(object sender, EventArgs e) {
            AddTimeTalk(DateTime.Now, false);
        }

        //���̎��񎞍����Z�b�g����
        internal void SetNextAlart() {
            if (_Settings.TimeSignal) {
                //���̎������Z�b�g
                DateTime dt = DateTime.Now;
                _NextAlartTime = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0).AddHours(1);
            } else {
                //���񖳌�
                _NextAlartTime = DateTime.MaxValue;
            }
        }

        //������ǂݏグ��
        private void AddTimeTalk(DateTime dt, bool bJustTime) {
            StringBuilder sb = new StringBuilder();
            if (bJustTime) {
                sb.Append("�|�b�@�@�|�b�@�@�|�b�@�@�p�A�A�A�A���@");
                sb.Append("�ڂ���݂����/��");
                sb.Append(dt.Hour);
                sb.Append("�����A����������'���B");
            } else {
                sb.Append(dt.Hour);
                sb.Append("��");
                sb.Append(dt.Minute);
                sb.Append("��");
                sb.Append(dt.Second);
                sb.Append("�b�ł��B");
            }
            Pub.AddTalkTask(sb.ToString(), -1, -1, VoiceType.Default);
        }

        #endregion


        #region ���N���X�E�\����

        // �ݒ�N���X�i�ݒ��ʕ\���E�t�@�C���ۑ����ȗ����Bpublic�ȃ����o�����ۑ������BXmlSerializer�ŏ����ł���N���X�̂ݎg�p�B�j
        public class Settings_Zihou : SettingsBase {
            //�ۑ��������i�ݒ��ʂ�����Q�Ƃ����j
            public bool TimeSignal = true;

            //�쐬���v���O�C��
            internal Plugin_Zihou Plugin;

            //�R���X�g���N�^
            public Settings_Zihou() {
            }

            //�R���X�g���N�^
            public Settings_Zihou(Plugin_Zihou pZihou) {
                Plugin = pZihou;
            }

            //GUI�Ȃǂ��瓖�I�u�W�F�N�g�̓ǂݍ���(�ݒ�Z�[�u���E�ݒ��ʕ\�����ɌĂ΂��)
            public override void ReadSettings() {
                
            }

            //���I�u�W�F�N�g����GUI�Ȃǂւ̔��f(�ݒ胍�[�h���E�ݒ�X�V���ɌĂ΂��)
            public override void WriteSettings() {
                Plugin.SetNextAlart();
            }
        }

        // �ݒ��ʕ\���p�N���X�i�ݒ��ʕ\���E�t�@�C���ۑ����ȗ����Bpublic�ȃ����o�����ۑ������BXmlSerializer�ŏ����ł���N���X�̂ݎg�p�B�j
        public class SettingFormData_Zihou : ISettingFormData {
            Settings_Zihou _Setting;

            public string       Title     { get { return _Setting.Plugin.Name; } }
            public bool         ExpandAll { get { return false; } }
            public SettingsBase Setting   { get { return _Setting; } }

            public SettingFormData_Zihou(Settings_Zihou setting) {
                _Setting = setting;
                PBase    = new SBase(_Setting);
            }

            //�ݒ��ʂŕ\�������N���X(ISettingPropertyGrid)
            public SBase PBase;
            public class SBase : ISettingPropertyGrid {
                Settings_Zihou _Setting;
                public SBase(Settings_Zihou setting) { _Setting = setting; }
                public string GetName() { return "����ݒ�"; }

                [Category   ("��{�ݒ�")]
                [DisplayName("01)�����L���ɂ���")]
                [Description("�P���Ԗ��Ɏ�����񂹂邩�ǂ����B\n���^�C�~���O�͐��m�ł͂���܂���O�O�G")]
                public bool TimeSignal { get { return _Setting.TimeSignal; } set { _Setting.TimeSignal = value; } }

                /* ISettingPropertyGrid�ł͐ݒ��ʂł̕\�����ڂ��w��ł��܂��B
                [Category   ("����")]
                [DisplayName("�\����")]
                [Description("������")]
                [DefaultValue(0)]        //�f�t�H���g�l�F�����\������Ȃ�����
                [Browsable(false)]       //PropertyGrid�ŕ\�����Ȃ�
                [ReadOnly(true)]         //PropertyGrid�œǂݍ��ݐ�p�ɂ���
                string  �t�@�C���I��     ��[Editor(typeof(System.Windows.Forms.Design.FolderNameEditor),       typeof(System.Drawing.Design.UITypeEditor))]
                string  �t�H���_�I��     ��[Editor(typeof(System.Windows.Forms.Design.FileNameEditor),         typeof(System.Drawing.Design.UITypeEditor))]
                string  �����s��������� ��[Editor(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
                */
            }
        }

        #endregion
    }
}
