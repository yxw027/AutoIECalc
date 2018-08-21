﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using AutoIECalcPublic;
using AutoFrameWork;


namespace AutoIECalcCmd
{
    class WeiyaCalcProcess : CalcProc
    {
        public WeiyaCalcProcess()
        {
            config = ConfigSetting.Load(ConfigSetting.WeiyaConfigPath);
        }

        public override string ConvertBaseStationDataToGPB()
        {
            Log.INFO(string.Format("START convert base station data to gpb!"));

            ClearFile(config.GetRawBaseStationDir(), "*.gpb");

            Application app = Application.Launch(config.GetConvetGPBExePath());

            Window convertWin = app.FindWindow("Convert Raw GNSS data to GPB");
            convertWin.GetByIndex<Editor>(0).SetValue(config.GetRawBaseStationDir());
            convertWin.Get<Button>("Add All").Click();

            //string rawBasePath = (from x in Directory.EnumerateFiles(config.GetRawBaseStationDir(), "*.18o")
            //                 select x).First();
            
            //convertWin.Get<ListItem>(rawBasePath).Click();
            //convertWin.Get<Button>("Options").Click();

            //Window rinexOptionWin = Window.Find("RINEX Options");
            //rinexOptionWin.Get<Button>("Static").Click();
            //rinexOptionWin.Get<Button>("OK").Click();

            convertWin.Get<Button>("Convert").Click();

            app.FindWindow("Converting RINEX to GPB (1/1)");

            Window completeWin = app.FindWindow("Conversion Complete (1/1 files succeeded)");
            convertWin.Get<Button>("Close").Click();

            app.Exit();

            string output = (from x in Directory.EnumerateFiles(config.GetRawBaseStationDir(), "*.gpb")
                             select x).First();

            Log.INFO(string.Format("SUCCESS convert base station data to gpb! output[{0}]", output));
            return output;
        }

        public override string ConvertRoverGNSSDataToGDB()
        {
            Log.INFO(string.Format("START convert rover gnss data to gpb!"));

            ClearFile(config.GetRawRoverGNSSDir(), "*.gpb");

            Application app = Application.Launch(config.GetConvetGPBExePath());

            Window convertWin = app.FindWindow("Convert Raw GNSS data to GPB");
            convertWin.GetByIndex<Editor>(0).SetValue(config.GetRawRoverGNSSDir());

            //string name = (from x in Directory.EnumerateFiles(config.GetRawRoverGNSSDir(), "*.TXT")
            //                      where x.Contains("_T.TXT")
            //                      select x).Single();
            //name = name.Substring(name.LastIndexOf(@"\")+1);
            //convertWin.Get<ListBox>(name).Click();
            //convertWin.Get<Button>("Add").Click();

            //Window detectWin = app.FindWindow("Auto Detect");
            //detectWin.Get<Button>("是(Y)").Click();
            convertWin.Get<Button>("Add All").Click();
            convertWin.Get<Button>("Convert").Click();

            app.FindWindow("Converting NovAtel OEM/SPAN to GPB (1/2)");

            Window completeWin = app.FindWindow(By.NameContains("Conversion Complete"));
            convertWin.Get<Button>("Close").Click();

            app.Exit();

            string output = (from x in Directory.EnumerateFiles(config.GetRawRoverGNSSDir(), "*.gpb")
                             select x).First();

            Log.INFO(string.Format("SUCCESS convert rover gnss data to gpb! output[{0}]", output));
            return output;
        }

        public override void DifferentialGNSS(string basestationFle, string GNSSFile)
        {
            Log.INFO(string.Format("START differential gnss"));

            processIE = Application.Launch(config.GetIE860ExePath());
            processIE.FindWindow("Version 8.60").WaitExit();


            Window dowloadWin = processIE.TryFindWindow("Download Manufacturer Files");
            if (dowloadWin != null)
            {
                dowloadWin.Get<Button>("Close").Click();
            }

            Window mainWin = processIE.FindWindow("Waypoint - Inertial Explorer 8.60");
            mainWin.GetByIndex<ToolbarButton>(1).Click();

            Window wizardWin = processIE.FindWindow("Project Wizard");
            wizardWin.Get<Button>("下一步(N) >").Click();
            Thread.Sleep(1000);

            wizardWin.Get<Button>("Create").Click();

            Window projectWin = processIE.FindWindow("Enter Project File");
            projectWin.GetByIndex<Editor>(0).SetValue(config.GetProjectCfgPath());
            projectWin.Get<Button>("保存(S)").Click();

            wizardWin.Get<Button>("下一步(N) >").Click();
            wizardWin.GetS<Button>("Browse")[0].Click();

            Window GNSSWin = processIE.FindWindow("Select GNSS File");
            GNSSWin.GetByIndex<Editor>(0).SetValue(GNSSFile);
            GNSSWin.GetByIndex<Editor>(0).GetValue();
            GNSSWin.Get<Button>("打开(O)").Click();

            Thread.Sleep(1000);

            string value = wizardWin.GetByIndex<Editor>(0).GetValue();
            if (value.Length == 0)
            {
                throw new Exception("GNSS File path is NULL");
            }

            wizardWin.Get<Button>("I have IMU data file in Waypoint (IMR) format").Click();
            wizardWin.GetS<Button>("Browse")[1].Click();

            Window IMUWin = processIE.FindWindow("Select IMU File (Waypoint Format)");
            IMUWin.GetByIndex<Editor>(0).SetValue(config.GetIMUFilePath());
            IMUWin.Get<Button>("打开(O)").Click();

            Thread.Sleep(1000);

            wizardWin.Get<Button>("下一步(N) >").Click();
            Window ConfirmWin = processIE.FindWindow("Add DMR File");
            ConfirmWin.Get<Button>("是(Y)").Click();

            wizardWin.Get<Button>("下一步(N) >").Click();
            wizardWin.Get<Button>("I would like to add base station data").Click();
            wizardWin.Get<Button>("下一步(N) >").Click();
            wizardWin.Get<ListBox>("Add Station from File").Click();
            wizardWin.Get<Button>("下一步(N) >").Click();

            Window childWin = processIE.FindWindow("Project Wizard");
            childWin.Get<Button>("Browse").Click();

            GNSSWin = processIE.FindWindow("Select GNSS File");
            GNSSWin.GetByIndex<Editor>(0).SetValue(basestationFle);
            
            GNSSWin.Get<Button>("打开(O)").Click();

            Thread.Sleep(1000);

            childWin.Get<Button>("下一步(N) >").Click();

            Action<Window> actionNomarl = (Window masterStationWin) =>
            {
                masterStationWin.GetByIndex<Editor>(2).SetValue(config.Lat[0]);
                masterStationWin.GetByIndex<Editor>(3).SetValue(config.Lat[1]);
                masterStationWin.GetByIndex<Editor>(4).SetValue(config.Lat[2]);

                masterStationWin.GetByIndex<Editor>(5).SetValue(config.Lon[0]);
                masterStationWin.GetByIndex<Editor>(6).SetValue(config.Lon[1]);
                masterStationWin.GetByIndex<Editor>(7).SetValue(config.Lon[2]);

                masterStationWin.GetByIndex<Editor>(8).SetValue(config.BasetStationHeight);
                masterStationWin.GetByIndex<Editor>(10).SetValue(config.AntennaMeasureHeight);

                masterStationWin.GetByIndex<ComboBox>(4).Select("Generic");

                Thread.Sleep(1000);
            };

            Action<Window> actionException = (Window errorWin) =>
            {
                string desc = errorWin.GetByIndex<StaticText>(0).GetValue();
                if(!desc.Contains("No antenna with the name"))
                {
                    throw new Exception(desc);
                }

                Log.WARN("Record message:" + desc);

                errorWin.Get<Button>("确定").Click();
                Window masterStationWin = processIE.FindWindow("Master Station Position");
                actionNomarl(masterStationWin);
            };

            Thread.Sleep(3000);
            processIE.FindChildWindow("Master Station Position",
                                      actionNomarl,

                                      "Error",
                                      actionException);

            childWin.Get<Button>("下一步(N) >").Click();

            wizardWin.Get<ListBox>("Finish").Click();

            wizardWin.Get<Button>("下一步(N) >").Click();
            //wizardWin.Get<Button>("下一步(N) >").Click();
            wizardWin.Get<Button>("完成").Click();

            mainWin.GetByIndex<ToolbarButton>(11).Click();
            Window processWin = processIE.FindWindow("Process GNSS");
            processWin.Get<Button>("Process").Click();

            processWin = processIE.FindWindow("Differential GNSS Preprocessing ...");
            processWin.WaitExit(5*60, ()=>
                                    {
                                        ListView listView = processWin.TryGet<ListView>(0);
                                        if (listView != null && listView.itemCount != 0)
                                        {
                                            string[] infos = listView.AllItem();
                                            foreach (string info in infos)
                                            {
                                                Log.WARN("Differential GNSS Preprocessing report:" + info);
                                            }

                                            processWin.Get<Button>("Continue").Click();
                                            processWin.WaitExit();
                                        }
                                    });

            Window processWin1 = processIE.FindWindow(By.NameContains("Processing Differential GPS 1"));
            Window processWin2 = processIE.FindWindow(By.NameContains("Processing Differential GPS 2"));
            processWin1.WaitExit();
            processWin2.WaitExit();

            Log.INFO(string.Format("SUCCESS differential gnss"));
            
        }

        public override void TightlyCoupleGNSSAndIMU()
        {
            Log.INFO(string.Format("START Tightly couple"));

            Application app = Application.FindProcess("wGpsIns");
            Window mainWin = app.FindWindow(By.NameContains("Inertial Explorer"));
            mainWin.GetByIndex<ToolbarButton>(14).Click();

            Window tightWin = app.FindWindow("Process Tightly Coupled");
            //tightWin.Get<Button>("Advanced GNSS").Click();

            //Window GNSSetingDialog = Window.Find("TC GNSS Settings");
            //{
            //    GNSSetingDialog.GetByIndex<TabCtrl>(0).Select("Measurement");
            //    GNSSetingDialog.GetByIndex<Editor>(0).SetValue("2.00");
            //    GNSSetingDialog.Get<Button>("确定").Click();
            //}

            //tightWin.Get<Button>("Advanced IMU").Click();
            //Window IMUSetingDialog = tightWin.FindChildWindow("IMU Processing Settings");
            //{
            //    Thread.Sleep(1000);
            //    //IMUSetingDialog.GetByIndex<TabCtrl>(0).Select("Alignment");
            //    IMUSetingDialog.GetS<Button>("Options")[0].Click();

            //    Window AligmentDialog = Window.Find("Alignment Options");
            //    AligmentDialog.Get<Button>("Automated alignment").Click();

                

            //    AligmentDialog.Get<Button>("OK").Click();
            //    AligmentDialog.WaitExit();

            //    IMUSetingDialog.GetS<Button>("Options")[1].Click();
            //    AligmentDialog = Window.Find("Alignment Options");
            //    AligmentDialog.Get<Button>("Automated alignment").Click();
            //    AligmentDialog.Get<Button>("OK").Click();
            //    AligmentDialog.WaitExit();

            //    IMUSetingDialog.GetByIndex<TabCtrl>(0).Select("GNSS");
            //    IMUSetingDialog.GetByIndex<Editor>(0).SetValue("1.00");

            //    IMUSetingDialog.Get<Button>("确定").Click();
            //}

            tightWin.GetByIndex<Editor>(0).SetValue(config.LeverArmOffsetX);
            tightWin.GetByIndex<Editor>(1).SetValue(config.LeverArmOffsetY);
            tightWin.GetByIndex<Editor>(2).SetValue(config.LeverArmOffsetZ);

            tightWin.Get<Button>("Process").Click();
            Window processWin = app.FindWindow("Tightly Coupled Differential Preprocessing ...");
            processWin.WaitExit(5 * 60, () =>
                                        {
                                            ListView listView = processWin.TryGet<ListView>(0);
                                            if (listView != null && listView.itemCount != 0)
                                            {
                                                string[] infos = listView.AllItem();
                                                foreach (string info in infos)
                                                {
                                                    Log.WARN("Tightly Coupled Differential Preprocessing report:" + info);
                                                }

                                                processWin.Get<Button>("Continue").Click();
                                                processWin.WaitExit();
                                            }
                                        });

            Window childWin1 = app.FindWindow(By.NameContains("Processing GPS-IMU TC 1"));
            Window childWin2 = app.FindWindow(By.NameContains("Processing GPS-IMU TC 2"));
            childWin1.WaitExit(10*60);
            childWin2.WaitExit();

            Thread.Sleep(2000);

            mainWin.GetByIndex<ToolbarButton>(4).Click();
            Thread.Sleep(2000);

            Log.INFO(string.Format("SUCCESS Tightly couple"));
        }

        public override string ExportPostTFile()
        {
            Log.INFO(string.Format("START Export PostT File"));
            Application app = Application.FindProcess("wGpsIns");
            Window mainWin = app.FindWindow(By.NameContains("Inertial Explorer"));
            mainWin.GetByIndex<ToolbarButton>(20).Click();

            Window exportWin = app.FindWindow("Export Coordinates Wizard");
            exportWin.Get<ListBox>("HUACE_Pos").Click();
            exportWin.Get<Button>("Epochs").Click();

            string output = config.GetPostprocessPath();
            exportWin.GetByIndex<Editor>(0).SetValue(output);
            Thread.Sleep(2000);

            exportWin.Get<Button>("下一步(N) >").Click();
            Thread.Sleep(2000);

            exportWin.Get<Button>("下一步(N) >").Click();
            Thread.Sleep(2000);

            exportWin.Get<Button>("下一步(N) >").Click();
            Thread.Sleep(2000);

            exportWin.Get<Button>("下一步(N) >").Click();
            Thread.Sleep(2000);

            exportWin.Get<Button>("下一步(N) >").Click();
            Thread.Sleep(2000);

            exportWin.Get<Button>("完成").Click();

            Window ProcessWin = app.FindWindow(output);
            ProcessWin.WaitExit();

            Thread.Sleep(3000);

            Log.INFO(string.Format("SUCESS Export PostT File"));
            return output;
        }

        private void ClearFile(string strDir, string pattern)
        {
            string[] filenames = (from x in Directory.EnumerateFiles(strDir, pattern)
                                  select x).ToArray();
            foreach (var filename in filenames)
            {
                File.Delete(filename);
            }

            Log.INFO(string.Format("Clear old file:[{0}]", string.Join(", ", filenames)));
        }
    }
}
