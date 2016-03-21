using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;

namespace FileSorting
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        #region Variables

        /// <summary>
        /// List of rules
        /// </summary>
        private readonly ObservableCollection<Regel> _regeln;

        /// <summary>
        /// List of Filetypes in selected Folder
        /// </summary>
        private ObservableCollection<Tuple<string, string, string>> _dateitypen;

        /// <summary>
        /// List of preknown Filetypes to show information
        /// </summary>
        private ObservableCollection<Tuple<string, string, string>> _dateitypenVorgabe;

        /// <summary>
        /// Stores the Files in the selected Folder in an String List because FileInfo List is not possible
        /// because the FolderPaths are to long sometimes
        /// </summary>
        private List<string> _dateiliste;

        /// <summary>
        /// Source Folder from where the Files should be taken
        /// </summary>
        private string _eingabepfad;

        /// <summary>
        /// stores if the Subfolders should be readed
        /// </summary>
        private bool _unterordner;

        #endregion

        /// <summary>
        /// Simple constructor initializes everything
        /// </summary>
        public MainWindow()
        {
            //initializes UI
            InitializeComponent();

            //initialize lists
            _regeln = new ObservableCollection<Regel>();
            _dateitypen = new ObservableCollection<Tuple<string, string, string>>();
            DateitypenVorgabeErstellen();

            //set subfolder true because it is used most times
            CbUnterordner.IsChecked = true;

            //setting data context for lists
            DataContext = this;
        }

        #region called by UIElements

        /// <summary>
        /// called on a button click (add rule, folder browser, start)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            //retrieves the clicked button
            System.Windows.Controls.Button btn = (System.Windows.Controls.Button) sender;

            if (Equals(btn, BtnPfad1))
            {
                //button for actual ruel path sets text into path text box
                TbAusgabePfad.Content = FolderBrowser();
            }
            else if (Equals(btn, BtnPfad2))
            {
                //button for source path sets text into variable
                TbEingabepfad.Content = FolderBrowser();
                _eingabepfad = TbEingabepfad.Content.ToString();

                Console.WriteLine(_eingabepfad);
                //store checkbox in variable (subfolders?)
                if (CbUnterordner.IsChecked != null) _unterordner = CbUnterordner.IsChecked.Value;

                //starts thread to scan all files, so that there Filetypes can be stored
                //Thread is used to not freez ui
                Thread tt = new Thread(DateitypenAktualisieren);
                tt.Start();
            }
            else if (Equals(btn, BtnRegel))
            {
                //adds the rule
                RegelHinzufuegen();
            }
            else if (Equals(btn, BtnStart))
            {
                //start the file moving
                Start();
            }
        }

        /// <summary>
        /// called by combox selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComBoxAuswahl(object sender, SelectionChangedEventArgs e)
        {
            //retrieves combox, add it's text to label
            System.Windows.Controls.ComboBox combo = (System.Windows.Controls.ComboBox)sender;
            TbDateitypen.Content +=  _dateitypen[combo.SelectedIndex].Item1 + ";";
        }

        #endregion

        #region inner logic

        /// <summary>
        /// adding a rule (called by add rule button
        /// </summary>
        private void RegelHinzufuegen()
        {
            //filetypes of this rules from label
            string dateitypen = TbDateitypen.Content.ToString();
            //output path from label
            string ausgabepfad = TbAusgabePfad.Content.ToString();

            //split filetypes at ';' and store them to list
            List<string> dateitypenList = dateitypen.Split(';').ToList();

            //create a new rule and add it to rule list
            Regel regel = new Regel(dateitypenList, ausgabepfad);
            _regeln.Add(regel);

            //tb dateitypen und ausgabepfad "leeren"
            TbDateitypen.Content = "";
            TbAusgabePfad.Content = "Move to Path";

            //update shown rule list
            ListeUpdaten();
        }

        /// <summary>
        /// Used to browse folders
        /// </summary>
        /// <returns></returns>
        private static string FolderBrowser()
        {
            //creates new folderbrowser
            var dialog = new FolderBrowserDialog();
            //shows it as dialog
            DialogResult result = dialog.ShowDialog();
            //retreives result
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //if result is ok folderpath is returned as string
                string folder = dialog.SelectedPath;
                return folder;
            }
            //else empty string is returned
            return "";
        }

        /// <summary>
        /// called by start btn to check inputs
        /// </summary>
        private void Start()
        {
            //sets variable of input path
            _eingabepfad = TbEingabepfad.Content.ToString();

            //checks if source path is availiable
            if (!Directory.Exists(_eingabepfad))
            {
                //sets error on btnstart
                BtnStart.Content = "Sourcepath does not exist!";
                return;
            }

            //sets variable for subfolders
            if (CbUnterordner.IsChecked != null) _unterordner = CbUnterordner.IsChecked.Value;

            Console.WriteLine(_dateiliste.Count);

            PbFortschritt.Maximum = _dateiliste.Count;
            //starts action (moving items) thread is used to show progress
            Thread t = new Thread(Aktion);
            t.Start();
        }

        /// <summary>
        /// moves files
        /// </summary>
        private void Aktion()
        {
            //check each file in filelist
            foreach (string file in _dateiliste)
            {
                int index = _dateiliste.IndexOf(file);
               

                //get extension and filename from file
                string extension = Path.GetExtension(file).ToUpper();
                string originalextension = Path.GetExtension(file);
                string fileName = Path.GetFileName(file);

                //check each rule
                foreach (Regel regel in _regeln)
                {   
                   
                    string newfile = regel.Pfad + "\\" + fileName;

                    //check extension
                    if (regel.DateitypenPublic.Contains(extension) && !string.IsNullOrEmpty(extension) && !string.IsNullOrWhiteSpace(extension))
                    {
                        try
                        {
                            //file gets an (x) before extension if it already exists
                            int x = 0;
                            if (File.Exists(newfile))
                            {
                                newfile = newfile.Replace(originalextension, "(" + 0 + ")" + extension);
                            }
                            bool f = true;
                            while (f)
                            {
                                if (File.Exists(newfile))
                                {
                                    newfile = newfile.Replace("(" + (x) + ")", "(" + (x + 1) + ")");

                                    if (File.Exists(newfile))
                                    {
                                        x++;
                                        f = true;
                                    }
                                    else
                                    {
                                        f = false;
                                    }
                                }
                                else
                                {
                                    f = false;
                                }

                            }
                            //if rule contains extension it is moved
                            File.Move(file, newfile);
                            //Update progress bar 

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(file + " | " + regel.Pfad + "\\" + fileName);
                        }
                        //breaks rule loop because it is not necessary to check next rule but go to next file
                        break;
                    }
                }
                UpdateStatus(index);
                //Console.WriteLine(index + " | " + file);
                Thread.Sleep(10);
            }

        }

        /// <summary>
        /// scans selected source folder for filetypes
        /// </summary>
        private void DateitypenAktualisieren()
        {
            //check if directoy exists else return
            if (!Directory.Exists(_eingabepfad)) return;

            //creates new file list (because maybe there is an old one to ovveride)
            _dateiliste = new List<string>();
            //and a new filetype list too
            _dateitypen = new ObservableCollection<Tuple<string, string, string>>();

            //to store subfolder and their files
            List<string[]> allFiles = new List<string[]>();

            //retreives files from this folder
            allFiles.Add(Directory.GetFiles(_eingabepfad, "*.*", SearchOption.TopDirectoryOnly));

            //retreives subfolders
            string[] directories = Directory.GetDirectories(_eingabepfad);

            //search in all subfolder
            foreach (string directory in directories)
            {
                //when in one subfolder is an exception it will skip to next and not abort complete program
                try
                {
                    Directory.GetAccessControl(directory);
                    //add files in folder to folder file
                    allFiles.Add(Directory.GetFiles(directory, "*.*", _unterordner ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
                }
                catch (Exception ex)
                {
                    // ReSharper disable once LocalizableElement & output directory and exception
                    Console.WriteLine(@"Skipped directory {0} with Exception: " + "\n" + ex, directory);
                }
            }


            //subfolder list
            foreach (var item in allFiles)
            {
                //the subfolders files
                foreach (var item2 in item)
                {
                    //adding to file list
                    _dateiliste.Add(item2);

                    //get extension
                    string extension = Path.GetExtension(item2);

                    //checks if extension is null, if then continues to next file
                    if (extension == null) continue;

                    //to check if filetype is in preknown filetypes list change it a bit (eg: .mp3 goes to MP3)
                    string extension2 = extension.Replace(".", "").ToUpper();

                    //checks if filetype is already in list
                    bool contains = _dateitypen.Any(c => c.Item1.Contains("." + extension2));

                    if (contains) continue;
                    //filetype not in list
                    
                       
                    //check if preknown list contains this file type
                    bool contains2 = _dateitypenVorgabe.Any(c1 => c1.Item1.Contains(extension2));

                    if (contains2)
                    {
                        //if it contains it search for item
                        foreach (var c1 in _dateitypenVorgabe)
                        {
                            if (c1.Item1.Equals(extension2))
                            {
                                
                                //add to filetypes with information
                                _dateitypen.Add(new Tuple<string, string, string>("." + extension2, c1.Item2, c1.Item3));
                            }
                        }
                    }
                    else
                    {
                        //add to filetypes without information
                        _dateitypen.Add(new Tuple<string, string, string>("." + extension2, "", ""));
                    }


                }
            }

            //updates combox which is showing filetypes
            ComboxUpdaten();

            
        }

        #endregion

        #region to update UIElemnts in Invoke to catch thread exceptions

        /// <summary>
        /// update rule list
        /// </summary>
        private void ListeUpdaten()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                RegelnListView.ItemsSource = _regeln;
                CollectionViewSource.GetDefaultView(RegelnListView.ItemsSource).Refresh();
            }));
        }

        /// <summary>
        /// update shown filetypes on combox
        /// </summary>
        private void ComboxUpdaten()
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (Tuple<string, string, string> item in _dateitypen)
                {
                    CbDateitypen.Items.Add(item.Item1 + " | " + item.Item2 + " | " + item.Item3);
                }
            }));
            
        }

        /// <summary>
        /// update progress bar
        /// </summary>
        /// <param name="fortschritt"></param>
        private void UpdateStatus(int fortschritt)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                
                PbFortschritt.Value = fortschritt;
            }));
        }

        #endregion

        #region Storage

        /// <summary>
        /// stores the preknown filetypes in the list to show information
        /// in combox which shows filetypes in selected folder
        /// </summary>
        private void DateitypenVorgabeErstellen()
        {
            _dateitypenVorgabe = new ObservableCollection<Tuple<string, string, string>>
            {
                new Tuple<string, string, string>("255", "Unix", "SBIG Bildformat"),
                new Tuple<string, string, string>("301", "Dos", "FAX Datei (Super FAX 2000)"),
                new Tuple<string, string, string>("3ds", "Windows", "Datendatei (3D Studio)"),
                new Tuple<string, string, string>("669", "Dos", "Musikmodul mit Samples (DualModulPlayer)"),
                new Tuple<string, string, string>("8bf", "Windows", "PlugIn (Adobe PhotoShop)"),
                new Tuple<string, string, string>("ABR", "Windows", "Pinselformen (Photo Shop)"),
                new Tuple<string, string, string>("ACB", "", "Grafikdatei (ACMB)"),
                new Tuple<string, string, string>("ACL", "Windows", "Autokorrekturliste (Winword, Office 97)"),
                new Tuple<string, string, string>("ACM", "Windows", "Advanced Color Driver Modul"),
                new Tuple<string, string, string>("ACV", "Windows", "Audio CODEC Datei (Creative Labs)"),
                new Tuple<string, string, string>("AD", "Windows", "Screen-Saver-Datei (After Dark)"),
                new Tuple<string, string, string>("ADC", "Dos", "Bitmap / 16 Farben (Scanstudio)"),
                new Tuple<string, string, string>("ADI", "Dos,Windows", "Plot Datei (AutoCAD)"),
                new Tuple<string, string, string>("ADN", "Dos", "Add In Utility (Lotus 1-2-3)"),
                new Tuple<string, string, string>("AFL", "Dos", "Fonts für Allways (Lotus)"),
                new Tuple<string, string, string>("AFM", "Windows", "Adobe Postscript Font Metrics (Adobe)"),
                new Tuple<string, string, string>("AI", "Windows", "Adobe Illustrator Graphikformat (PostScript)"),
                new Tuple<string, string, string>("AIF", "Mac", "Audio Interchange Dateiformat oder Information"),
                new Tuple<string, string, string>("AIFC", "Mac", "Audio Interchange Dateiformat oder Information"),
                new Tuple<string, string, string>("AIFF", "Mac", "Audio Interchange Dateiformat oder Information"),
                new Tuple<string, string, string>("ANI", "Atari / Windows", "Film/zusätzliches NEO-Bild nötig (NeoChrome) / animierter Cursor (Windows 9x, NT 3.5x)"),
                new Tuple<string, string, string>("ANS", "Dos", "ANSI-Textdatei, ANSI-Grafik"),
                new Tuple<string, string, string>("APD", "", "Aldus Printer Description"),
                new Tuple<string, string, string>("ARC", " / Windows", "Archiv (ARC, PKARC, PKXARC, PKPAK, LHARC) / Archiv (Schedule Windows NT 3.51)"),
                new Tuple<string, string, string>("ARJ", "Dos", "Archiv (ARJ Packer)"),
                new Tuple<string, string, string>("ART", "Atari", "Rasterdatei Graphic (Art Director)"),
                new Tuple<string, string, string>("ASF", "Windows,Mac", "Video Streaming File"),
                new Tuple<string, string, string>("ASP", "Windows", "Dynamische ASP (Microsoft Active Server Pages)"),
                new Tuple<string, string, string>("ATT", "", "AT&T-Bitmap-Fax-Format"),
                new Tuple<string, string, string>("AU", "Sun", "Sounddatei (Audio Basic)"),
                new Tuple<string, string, string>("AVI", "Windows",
                    "Audio Video Interleaved, Animated Video File / RIFF (Video für Windows)"),
                new Tuple<string, string, string>("B8", "Dos", "PicLab - Farbwerte eines 24-bit Bildes"),
                new Tuple<string, string, string>("BBM", "Amiga,Dos", "IFF-Brush (Deluxe Paint)"),
                new Tuple<string, string, string>("BFX", "Dos", "Fax-Datei (BitFax)"),
                new Tuple<string, string, string>("BIT", "", "Lotus BIT, Graphikdatei"),
                new Tuple<string, string, string>("BLD", "Dos", "BASIC Bload graphics"),
                new Tuple<string, string, string>("BMP", "OS/2,Win", "Bitmap Picture, BitMap Images (Paintbrush )"),
                new Tuple<string, string, string>("BRD", "Dos", "Eagle Layout File (Eagle)"),
                new Tuple<string, string, string>("brsh", "Amiga", "IFF-ILB-Brush (DPaint)"),
                new Tuple<string, string, string>("BSV", "Dos", "BASIC BSave graphics"),
                new Tuple<string, string, string>("C3D", "Windows", "3D - Vorlage (Ulead Cool 3D Version 2.0)"),
                new Tuple<string, string, string>("CAL", " / Dos", "CALS Compressed Bitmap / CALS Bitmap, Kalenderdatendatei (Calendar Creator für DOS von Softkey)"),
                new Tuple<string, string, string>("CAM", "", "Bilddatei, Casio Kameras (Casio)"),
                new Tuple<string, string, string>("CAP", "Dos", "Capture-Datei (Telix)"),
                new Tuple<string, string, string>("CCO", "Dos", "BTX-Grafik (XBTX)"),
                new Tuple<string, string, string>("CCS", "Windows", "Color Einstellung (Corel Draw)"),
                new Tuple<string, string, string>("CDA", "Windows",
                    "CD Audio Track (MP3 Player, CD - Player von Windows)"),
                new Tuple<string, string, string>("CDR", "Windows", "Vektorgrafik (CorelDraw)"),
                new Tuple<string, string, string>("CDT", "Windows", "Corel Symbol Infokey Modul / CorelDraw-Vorlage, Vektor (CorelDraw)"),
                new Tuple<string, string, string>("CE", "", "Graphikformat (Computer Eyes)"),
                new Tuple<string, string, string>("CED", "Windows", "Vektorgraphik (Arts & Letters)"),
                new Tuple<string, string, string>("CEG", "", "Edsun Continuous Edge Graphics"),
                new Tuple<string, string, string>("CGM", "Dos",
                    "Computer Graphics Metadatei, Vektorgraphik (Freelance / Vektor)"),
                new Tuple<string, string, string>("CIL", "", "Clip Gallery Download Paket"),
                new Tuple<string, string, string>("CIT", "", "CCITT group 4 graphics"),
                new Tuple<string, string, string>("CMF", "", "Corel Metafile (CorelDRAW)"),
                new Tuple<string, string, string>("CMF", "Dos", "Creative Musik File"),
                new Tuple<string, string, string>("CMX", "Windows", "CorelDraw Presentation Exchange"),
                new Tuple<string, string, string>("CNC", "", "CNC-Programmdateien Allgemein / ASCII"),
                new Tuple<string, string, string>("COL", "Dos", "Farbpalette (Autodesk Animator)"),
                new Tuple<string, string, string>("COT", "", "Continuous Tone Color Graphics"),
                new Tuple<string, string, string>("CPT", "Windows / Mac", "Corel Photo Paint Image / Archiv (CompactPro)"),
                new Tuple<string, string, string>("CPX", "Windows", "Corel CMX Komprimierung (Corel)"),
                new Tuple<string, string, string>("CRG", "Atari", "Raster-Grafik (Calamus)"),
                new Tuple<string, string, string>("CRL", "", "Color Run Length encoded graphics"),
                new Tuple<string, string, string>("CSV", "", "Textdatei mit Datensätzen durch Komma getrennt"),
                new Tuple<string, string, string>("CUR", "Windows", "Cursor-Grafik, Mauspfeile (Microangelo, Windows)"),
                new Tuple<string, string, string>("CUT", "Dos",
                    "Digital Research Halo Graphics, Bitmap (Dr.Halo), benötigte PAL-Datei"),
                new Tuple<string, string, string>("CVG", "Atari", "Vektorgrafik (Calamus)"),
                new Tuple<string, string, string>("CVP", "Windows", "Fax-Deckblatt (Delrina Winfax 3.0)"),
                new Tuple<string, string, string>("CVR", "Windows", "Fax-Deckblatt (Eclipse Fax SE)"),
                new Tuple<string, string, string>("CWG", "", "Claris Works Graphics"),
                new Tuple<string, string, string>("DCF", "", "Document Control File"),
                new Tuple<string, string, string>("DCL", "Windows", "Dialog Controll Language, Dialogfenster (AutoCAD)"),
                new Tuple<string, string, string>("DCS", "", "Desktop Color Separation (Fa. Quark) / Bilddatei, Kodak Kameras (Kodak)"),
                new Tuple<string, string, string>("DCX", "Windows", "Multipaged PCX-Datei (PCC oder Eclipse Fax SE)"),
                new Tuple<string, string, string>("DHP", "", "Dr Halo Picture (Dr. Halo)"),
                new Tuple<string, string, string>("DIB", "Windows", "Device-Independent Bitmap (Windows Pixel Format)"),
                new Tuple<string, string, string>("DIL", "Windows", "Lotus-Library für Grafikimport (Approach)"),
                new Tuple<string, string, string>("DL", "Dos", "Animation (DL-View)"),
                new Tuple<string, string, string>("DOC", "Atari / Dos,Windows", "Document (1st Word, Word Plus) / Text, Dokumentation (WinWord, MS-Office, 1st Word, Lotus Manuscipt, WordPerfect, Framemake)"),
                new Tuple<string, string, string>("DOK", "", "Textdatei mit Dokumentation"),
                new Tuple<string, string, string>("DOT", "Windows", "Document Template, Dokumentenvorlage (Win Word)"),
                new Tuple<string, string, string>("DS4", "Windows", "Zeichnung (Designer Version 4.x)"),
                new Tuple<string, string, string>("DWF", "Windows", "Drawing, Web Zeichnungsformat-Datei (AutoCAD)"),
                new Tuple<string, string, string>("DWG", "Dos,Windows", "Drawing Zeichnung (AutoCAD)"),
                new Tuple<string, string, string>("DWT", "Windows", "Zeichnungsvorlage (AutoCAD)"),
                new Tuple<string, string, string>("DXF", "Windows", "2-D Format (AutoCAD)"),
                new Tuple<string, string, string>("EMF", "Windows", "Enhanced Windows Metadatei"),
                new Tuple<string, string, string>("ENC", "", "Encoded Datei"),
                new Tuple<string, string, string>("epic", "Unix", "Latex Picture und Epic Macro"),
                new Tuple<string, string, string>("EPS", "","Encapsulated PostScript (Druckersprache von Adobe). Was ist EPS ?"),
                new Tuple<string, string, string>("EPI", "", "Encapsulated Postscript mit Raster Image (Adobe)"),
                new Tuple<string, string, string>("FAX", "Dos,Windows","Rastergraphic (FAX, PC-Tools, Trans-Send WinDOS)"),
                new Tuple<string, string, string>("FIG", "Unix", "Vektorgrafik"),
                new Tuple<string, string, string>("FIT", "", "NASA FITS-Bilder"),
                new Tuple<string, string, string>("FITS", "", "NASA FITS-Bilder"),
                new Tuple<string, string, string>("FLC", "Dos","Animation, Videosequenz im erweitertem FLIC-Format (AutoDesk Animator Pro / FLIC)"),
                new Tuple<string, string, string>("FLI", "Dos", "Animation, Videosequenz im speziellem FLIC-Format (AutoDesk Animator / FLIC) / TeX-Fontlib (EmTeX)"),
                new Tuple<string, string, string>("FLM", "Windows", "Rastergraphic (Screen Machine Digitizer-Steckkarte, Fast / Screenmachine)"),
                new Tuple<string, string, string>("FTS", "", "NASA FITS-Bilder"),
                new Tuple<string, string, string>("FXD", "Windows", "Faxdatei (Winfax)"),
                new Tuple<string, string, string>("G8", "Dos", "PicLab - Farbwerte eines 24-bit Bildes"),
                new Tuple<string, string, string>("GDF", "", "Graphics Data File"),
                new Tuple<string, string, string>("GED", "Windows", "Vektorgrafik (Arts & Letters)"),
                new Tuple<string, string, string>("GEM", "Atari", "MetaDatei (GEM)"),
                new Tuple<string, string, string>("GEM", "","Graphics Environment Manager Datei format (Digital Research's GEM Desktop & Ventura Publisher)"),
                new Tuple<string, string, string>("GFX", "", "Graphics (phbf, csbx)"),
                new Tuple<string, string, string>("GIF", "", "Graphics Interchange Format C 89a von CompuServe"),
                new Tuple<string, string, string>("GL", "Dos", "Animation (GLView / 320x200x256)"),
                new Tuple<string, string, string>("GPM", "", "Graphics Printing Management"),
                new Tuple<string, string, string>("GRF", "","Graphics File (Vektorgraphik, Graph Plus, Micrografx Charisma)"),
                new Tuple<string, string, string>("GSF", "", "Graphics Stream File"),
                new Tuple<string, string, string>("HDF", "", "Hierarchical Data Format Datei"),
                new Tuple<string, string, string>("HDR", "", "NASA FITS-Bilder"),
                new Tuple<string, string, string>("HGL", "Dos", "HP Graphics Language"),
                new Tuple<string, string, string>("HNC", "", "CNC-Programmdateien ASCII (Heidenhain)"),
                new Tuple<string, string, string>("HP", "Dos", "Hewlett Packard Graphics Language Datei"),
                new Tuple<string, string, string>("HPC", "Dos", "Hewlett Packard LaserJet Graphics"),
                new Tuple<string, string, string>("HPG", "Dos", "Druckausgabe Plotter (Havard Graphics)"),
                new Tuple<string, string, string>("ICC", "", "Intelligent Color Charting, Farbkalibrierung"),
                new Tuple<string, string, string>("ICL", "Windows", "Icon-Libraries (Microangelo)"),
                new Tuple<string, string, string>("ICM", "", "Intelligent Color Matching Library"),
                new Tuple<string, string, string>("ICN", "", "Icon Datei"),
                new Tuple<string, string, string>("ICO", "OS/2,Win", "Icon Datei"),
                new Tuple<string, string, string>("IFF", "Amiga", "IFF-ILBM (InterLeaved BitMap Grafik) / IFF-8SVX, 8 Samples Voices (Digi Sound)"),
                new Tuple<string, string, string>("IGF", "", "Inset Graphics Format (Inset Systems)"),
                new Tuple<string, string, string>("ilbm", "Amiga", "siehe IFF"),
                new Tuple<string, string, string>("IM", "", "KO-23 Satellitenbild mit 109-Block Fehlerkorrektur"),
                new Tuple<string, string, string>("IMG", "Atari / Dos", "Bitmap, compressed (GEM, Paint) / Image Datei Format,compressed Bitmap (Digital Research/GEM Desktop - Ventura Publisher)"),
                new Tuple<string, string, string>("IMQ", "", "komprimierte NASA-PDS-Dateien (Planetary Data System)"),
                new Tuple<string, string, string>("IVF", "Windows","Indeo Video Format (Intel Indeo Video 5.0 PD Plug-In für Netscape Navigator)"),
                new Tuple<string, string, string>("JBR", "Windows", "Pinselformen (Paint Shop Pro)"),
                new Tuple<string, string, string>("JFI", "", "Image/Graphics Format"),
                new Tuple<string, string, string>("JPE", "", "Joint Photographic Expert Group's Graphics Format"),
                new Tuple<string, string, string>("JPG", "", "JFIF-komprimierte Grafik (JPG: Joint Photographic Experts Group)"),
                new Tuple<string, string, string>("JTF", "Dos", "TIFF-Grafik mit JPEG Kompression"),
                new Tuple<string, string, string>("KDC", "", "Rastergraphic Kodak Kameras"),
                new Tuple<string, string, string>("KFX", "", "Kofax group 4 - S/W Pixelgraphik"),
                new Tuple<string, string, string>("KQP", "", "Rastergraphic Konica Kameras"),
                new Tuple<string, string, string>("LAY", "Windows", "Layer Einstellungen (Layer Manager, AutoCAD)"),
                new Tuple<string, string, string>("LBL", "", "NASA FITS-Bilder"),
                new Tuple<string, string, string>("lbm", "Amiga", "Grafik (Deluxe Paint / IFF-Standard)"),
                new Tuple<string, string, string>("LGO", "Dos,Win", "Grafik-Logo"),
                new Tuple<string, string, string>("MCS", "Dos", "Vektorgraphik (MathCAD)"),
                new Tuple<string, string, string>("MCW", "Mac", "Macintosh Word Datei (Word für Macintosh)"),
                new Tuple<string, string, string>("MDC", "", "Rastergraphic Minolta Kameras"),
                new Tuple<string, string, string>("MIC", "Windows", "Rastergraphic (Microsoft Image Composer)"),
                new Tuple<string, string, string>("MNC", "Windows", "Menüdatei (AutoCAD)"),
                new Tuple<string, string, string>("MNS", "Windows", "acad.mns, Menüdatei (AutoCAD)"),
                new Tuple<string, string, string>("MNU", "Windows", "acad.mnu, Menüvorlage (AutoCAD)"),
                new Tuple<string, string, string>("MNX", "Windows", "Menüdatei (AutoCAD)"),
                new Tuple<string, string, string>("MOV", "Windows", "Apple Quick Time Movie Format, Videodatei (Quicktime)"),
                new Tuple<string, string, string>("MPG", "", "Filmdatei. MPEG: Motion Picture Experts Group"),
                new Tuple<string, string, string>("MSP", "Dos","MicroSoft Paint - Grafik (Windows 2.0 Bitmap, Microsoft Paint)"),
                new Tuple<string, string, string>("MWF", "", "MoveFile (Corel)"),
                new Tuple<string, string, string>("NEO", "Atari", "NEOchrome Bitmap Graphics, RasterDatei (Neochrome)"),
                new Tuple<string, string, string>("OBT", "Windows", "Sammelmappen-Vorlage (Microsoft Office 97)"),
                new Tuple<string, string, string>("OBZ", "Windows", "Sammelmappen-Vorlage (Microsoft Office 97)"),
                new Tuple<string, string, string>("OCX", "Windows", "ActiveX Steuerelement, Visual Basic Programmdatei"),
                new Tuple<string, string, string>("OFN", "Windows", "Dokumente (Microsoft Office)"),
                new Tuple<string, string, string>("PAF", "Windows", "Photo Animator (Adobe PhotoShop)"),
                new Tuple<string, string, string>("PAL", "Dos", "Paletten-Datei (Dr.Halo / zu CUT-Datei)"),
                new Tuple<string, string, string>("PBM", "Unix", "Portable BitMap Datei (PBMPlus und NETPBM)"),
                new Tuple<string, string, string>("PCD", "", "Kodak Photo-CD Grafik / Format 768x512"),
                new Tuple<string, string, string>("PCL", "Windows",
                    "Druckdatei, Printer Communication Language (Laserjet, Hewlett Packard)"),
                new Tuple<string, string, string>("PCP", "Dos, Windows", "Plotparameter (AutoCAD)"),
                new Tuple<string, string, string>("PCT", "Mac",
                    "Picture - Bilddatei, das Macintosh-eigene Format für Vektorgrafiken"),
                new Tuple<string, string, string>("PCX", "",
                    "Picture Exchange Datei, ZSoft Image Datei (Bitmap Format von PC Paintbrush)"),
                new Tuple<string, string, string>("PDD", "Windows", "PhotoShop Datei (Adobe PhotoShop)"),
                new Tuple<string, string, string>("PDF", "", "Portable Document Format (Adope Acrobat Reader)"),
                new Tuple<string, string, string>("PFB", "Dos, Windows",
                    "PostScript Schriftart (AutoCAD, auch Adobe Type Manager)"),
                new Tuple<string, string, string>("PFM", "Windows", "Font Metric (Adope Type Manager)"),
                new Tuple<string, string, string>("PGM", "Unix", "Portable Grayscale Map Datei (PBMPlus und NETPBM)"),
                new Tuple<string, string, string>("PIC", "Dos,Windows",
                    "Picture Datei (BioRad Confocal, Lotus 1-2-3, PC Paint, PIXAR, SoftImage, Inset Systems). Unterschiedliche Formate."),
                new Tuple<string, string, string>("PLT", "Dos, Windows",
                    "Hewlett-Packard - PLotTer Print Datei (AutoCAD), HPGL"),
                new Tuple<string, string, string>("PMC", "Dos", "Grafik, A4TECH Scanner"),
                new Tuple<string, string, string>("PMP", "", "Rastergraphic Sony Kameras"),
                new Tuple<string, string, string>("PNG", "", "Portable Network Graphics"),
                new Tuple<string, string, string>("PNM", "Unix", "Portable aNy Map Datei (PBMPlus und NETPBM)"),
                new Tuple<string, string, string>("PNM", "Unix", "Portable Network Map 1,8,24 Bps"),
                new Tuple<string, string, string>("PNT", "Mac", "PainTing (MacPaint-Graphik)"),
                new Tuple<string, string, string>("PPB", "", "PostScript Treiber (Adobe)"),
                new Tuple<string, string, string>("PPD", "", "PostScript Printer Description"),
                new Tuple<string, string, string>("PPM", "Unix", "Portable Pixel Map Datei (PBMPlus und NETPBM)"),
                new Tuple<string, string, string>("PPS", "Windows", "Diashows (Power Point)"),
                new Tuple<string, string, string>("PPT", "Windows", "Power Point 95 & 97 Präsentation"),
                new Tuple<string, string, string>("PS", "", "PostScript-Druckdatei. Was ist das ?"),
                new Tuple<string, string, string>("PSD", "Windows", "PhotoShop Datei (Adobe PhotoShop)"),
                new Tuple<string, string, string>("PSF", "Windows",
                    "acad.psf, PostScript font substitution map (AutoCad)"),
                new Tuple<string, string, string>("PSZ", "Unix", "compressed PostScript Datei"),
                new Tuple<string, string, string>("QFX", "Dos", "Faxdatei (QuickLink)"),
                new Tuple<string, string, string>("RAS", "Sun", "Raster Picture Datei (Fa. Sun)"),
                new Tuple<string, string, string>("RAW", "Amiga", "Grafik oder Sound, unkomprimiert"),
                new Tuple<string, string, string>("ARW", "", "Grafik oder Sound, unkomprimiert"),
                new Tuple<string, string, string>("RG4", "", "Raw Group 4 (dasselbe wie .CIT)"),
                new Tuple<string, string, string>("RGB", "Unix", "RGB 24 bit color graphics (SGI)"),
                new Tuple<string, string, string>("RGBA", "Unix", "RGB 32 bit color graphics with Alpha Channel (SGI)"),
                new Tuple<string, string, string>("RIF", "", "Raster Image File o. Resource Interchange File Format"),
                new Tuple<string, string, string>("RIP", "Dos", "Metagrafik (RIPTerm)"),
                new Tuple<string, string, string>("RIX", "Dos",
                    "Pixelformat (von u.a. Fa. Colorix, Winrix, RIX-Present)"),
                new Tuple<string, string, string>("RLA", "Windows", "Raster Image Datei (Wavefront)"),
                new Tuple<string, string, string>("RLE", "Windows", "Windows Bitmap (BMP) mit RLE-Kompression"),
                new Tuple<string, string, string>("RLE", "Unix", "Run Length Encoded Graphics (Utah RLE)"),
                new Tuple<string, string, string>("RMI", "Dos", "RIFF MIDI Interchange Datei Format, Audio Objekt"),
                new Tuple<string, string, string>("RMM", "Windows",
                    "RAM Meta Datei, Multimedia - Video - Datei (Real PlayerG2)"),
                new Tuple<string, string, string>("RP", "Windows", "RealPix (Real PlayerG2)"),
                new Tuple<string, string, string>("RPB", "", "Raw Portable BitMap Datei"),
                new Tuple<string, string, string>("RPG", "", "Raw Portable Grayscale Map Datei"),
                new Tuple<string, string, string>("RPN", "", "Raw Portable aNy Map Datei"),
                new Tuple<string, string, string>("RPP", "", "Raw Portable Pixel Map Datei"),
                new Tuple<string, string, string>("RTF", "", "Rich Text Format"),
                new Tuple<string, string, string>("SC", "Dos", "Grafik (ColorRIX EGA Paint)"),
                new Tuple<string, string, string>("SDF", "Windows", "Clipart (AmiPro)"),
                new Tuple<string, string, string>("SFI", "", "Grafik (SIS Framegrabber)"),
                new Tuple<string, string, string>("SFW", "", "Bilddatei, ähnliche zu JPEG (Seattle FilmWorks)"),
                new Tuple<string, string, string>("SGF", "", "Textdatei mit Grafik (Starwriter)"),
                new Tuple<string, string, string>("SGI", "", "Bilddatei RLE (SGI)"),
                new Tuple<string, string, string>("SHD", "", "Spool Datei Header"),
                new Tuple<string, string, string>("SKD", "Windows", "Zeichnung (AutoCAD)"),
                new Tuple<string, string, string>("SKT", "Windows", "Vorlagedatei (AutoCAD)"),
                new Tuple<string, string, string>("SLB", "Windows", "Diabibliothek (AutoCAD)"),
                new Tuple<string, string, string>("SLD", "Windows", "SLiDe Vektorgraphik (AutoCAD)"),
                new Tuple<string, string, string>("SUN", "Sun", "Sun RasterDatei"),
                new Tuple<string, string, string>("SWF", "Windows", "Shockwave Flash (Real PlayerG2)"),
                new Tuple<string, string, string>("SYM", "Dos", "Symboldatei (Harvard Grafik)"),
                new Tuple<string, string, string>("TG4", "", "Tiled Group 4 Graphics"),
                new Tuple<string, string, string>("TGA", "Dos", "Grafik / Truevision TarGA Graphics Bitmap Datei Format"),
                new Tuple<string, string, string>("TGZ", "", "Archiv, Tar und GNUzip (tar.z)"),
                new Tuple<string, string, string>("TIF", "", "Tagged Image File Format / TIFF"),
                new Tuple<string, string, string>("TIFF", "", "Tagged Image File Format / TIFF"),
                new Tuple<string, string, string>("VBT", "", "VBiT Raster Data"),
                new Tuple<string, string, string>("VGA", "Dos", "VGA-Grafikdatei"),
                new Tuple<string, string, string>("VI", "", "Grafik (Jovian)"),
                new Tuple<string, string, string>("VIF", "", "Visualization Image Datei Format (Khoros)"),
                new Tuple<string, string, string>("WFN", "Windows", "Font Datei (CorelDraw)"),
                new Tuple<string, string, string>("WMF", "Windows", "Windows Metafile Format / Vektorgraphik"),
                new Tuple<string, string, string>("WPG", "Dos, Windows",
                    "Corel WordPerfect Graphic (DrawPerfect, WordPerfect)"),
                new Tuple<string, string, string>("XLA", "Windows", "Excel 3 Workbook, Makro-Vorlage (Excel)"),
                new Tuple<string, string, string>("XLC", "Windows", "Excel Chart Graphics, Diagramm (Excel / Chart)"),
                new Tuple<string, string, string>("XLS", "Windows", "Excel Spreadsheet Datei, Tabelle - Blatt (Excel)"),
                new Tuple<string, string, string>("XLT", "Windows", "Mustervorlage (Excel)"),
                new Tuple<string, string, string>("XLW", "Windows", "Arbeitsmappe (Excel)"),
                new Tuple<string, string, string>("XPM", "Unix", "X11-s/w-Bild im Rasterformat"),
                new Tuple<string, string, string>("xwd", "Unix", "X - Windows - Dump - Image - Datei"),
                new Tuple<string, string, string>("XX", "", "Image file (Stardent AVS)"),
                new Tuple<string, string, string>("", "", ""),
                new Tuple<string, string, string>("", "", ""),
                new Tuple<string, string, string>("32", "R", "Raw Yamaha DX7 32-voice data. S"),
                new Tuple<string, string, string>("3G2", "R+W", "3GPP 'project 2' file format. autowave*"),
                new Tuple<string, string, string>("3GP", "R+W", "3GPP file format. autowave*"),
                new Tuple<string, string, string>("404", "R+W", "Muon DS404 bank file. autocoll"),
                new Tuple<string, string, string>("404", "R+W", "Muon DS404 patch file. autoinst"),
                new Tuple<string, string, string>("669", "R", "669 modules. auto"),
                new Tuple<string, string, string>("AAC", "R+W",
                    "MPEG-2/4 Advanced Audio Coding format (ADTS container format). autowave*"),
                new Tuple<string, string, string>("AAC", "R",
                    "MPEG-2/4 Advanced Audio Coding format (ADIF container format). auto*"),
                new Tuple<string, string, string>("AAC", "R", "MPEG-2/4 Advanced Audio Coding format ('raw' format). *"),
                new Tuple<string, string, string>("AIFC", "R", "Compressed Audio Interchange File Format. auto"),
                new Tuple<string, string, string>("AIF", "R+W", "Audio Interchange File Format. autowave"),
                new Tuple<string, string, string>("AIFF", "R+W", "Audio Interchange File Format. autowave"),
                new Tuple<string, string, string>("AIS", "R", "Velvet Studio instrument. auto"),
                new Tuple<string, string, string>("AKAI", "R", "AKAI S-series floppy disk image file. auto"),
                new Tuple<string, string, string>("AKP", "R+W", "AKAI S-5000/S-6000 programs. autoinst"),
                new Tuple<string, string, string>("ALAW", "R+W", "G.711 A-law european telephony format. wave"),
                new Tuple<string, string, string>("ALW", "R+W", "G.711 A-law european telephony format. wave"),
                new Tuple<string, string, string>("AMS", "R / R + W", "Extreme Tracker modules. auto / Velvet Studio modules. auto / MIME 'AMR file storage' format. autowave"),
                new Tuple<string, string, string>("APE", "R+W", "Monkey Audio losslessly compressed file. autowave"),
                new Tuple<string, string, string>("APEX", "R+W", "AVM Sample Studio bank file. autocollinst"),
                new Tuple<string, string, string>("ARL", "R+W", "Aureal 'Aspen' bank file. autocollinstwave"),
                new Tuple<string, string, string>("ASE", "R", "Velvet Studio sample. auto"),
                new Tuple<string, string, string>("ASF", "R", "ActiveMovie streaming format. autowave"),
                new Tuple<string, string, string>("ATAK??", "R+W", "Soundscape Audio-Take file. autowave"),
                new Tuple<string, string, string>("AU", "R+W", "Sun/NeXT/DEC audio file. autowave"),
                new Tuple<string, string, string>("AVI", "R", "Microsoft Audio Video Interleave file. auto"),
                new Tuple<string, string, string>("AVR", "R", "Audio Visual Research sound file. auto"),
                new Tuple<string, string, string>("BN4", "R", "Yamaha DX21 / DX27 / DX100 voice SysEx dump. autoS"),
                new Tuple<string, string, string>("BNK", "R", "Yamaha DX11 / TX81z / DX21 / DX27 / DX100 voice SysEx dump. autoS / Ad Lib bank. autoS"),
                new Tuple<string, string, string>("BWF", "R", "Broadcast wave file. auto"),
                new Tuple<string, string, string>("C01", "R", "Typhoon wave file. auto"),
                new Tuple<string, string, string>("CAF", "R+W", "Apple CoreAudio file format. autowave"),
                new Tuple<string, string, string>("CAFF", "R+W", "Apple CoreAudio file format. autowave"),
                new Tuple<string, string, string>("CDA", "R", "Audio CD tracks. autoCD"),
                new Tuple<string, string, string>("CDR", "R", "Audio CD compatible raw data."),
                new Tuple<string, string, string>("CMF", "R", "Creative Labs music file. autoS"),
                new Tuple<string, string, string>("COD", "R+W", "3GPP 'AMR interface format 2'. wave"),
                new Tuple<string, string, string>("DCM", "R", "DCM modules. auto"),
                new Tuple<string, string, string>("DEWF", "R", "SoundCap/SoundEdit instrument."),
                new Tuple<string, string, string>("DIG", "R", "Digilink format. auto"),
                new Tuple<string, string, string>("DIG", "R", "Sound Designer I file."),
                new Tuple<string, string, string>("DLP", "R", "DirectMusic Producer DLS file. auto"),
                new Tuple<string, string, string>("DLS", "R+W", "DownLoadable Sounds level 1/2/2+/2++. autocollinstwave / Mobile DLS file minimal / full feature set. autocollinstwave"),
                new Tuple<string, string, string>("DMF", "R", "Delusion/XTracker digital music format. auto"),
                new Tuple<string, string, string>("DR8", "R+W", "FXpansion DR-008 drumkits. autoinst"),
                new Tuple<string, string, string>("DRO", "R", "DOSBox Raw OPL format. auto"),
                new Tuple<string, string, string>("DSF", "R", "Delusion/XTracker sample format. auto"),
                new Tuple<string, string, string>("DSM", "R", "Digital Sound Module format. auto"),
                new Tuple<string, string, string>("DSS", "R", "Olympus DSS file. auto*"),
                new Tuple<string, string, string>("DTM", "R", "DigiTrekker modules. auto"),
                new Tuple<string, string, string>("DWD", "R+W", "DiamondWare digitized file. auto"),
                new Tuple<string, string, string>("DX7", "R", "Yamaha DX7 voice SysEx dump. autoS"),
                new Tuple<string, string, string>("DX7", "R", "Raw Yamaha DX7 32-voice data. S"),
                new Tuple<string, string, string>("EDA", "R", "Ensoniq ASR-10 disk image. autoFD CD"),
                new Tuple<string, string, string>("EDE", "R", "Ensoniq EPS disk image. autoFD CD"),
                new Tuple<string, string, string>("EDK", "R", "Ensoniq KT disk image. auto"),
                new Tuple<string, string, string>("EDM", "R", "Ensoniq Mirage disk image."),
                new Tuple<string, string, string>("EDQ", "R", "Ensoniq SQ1/SQ2/KS32 disk image. auto"),
                new Tuple<string, string, string>("EDV", "R", "Ensoniq VFX-SD disk image. auto"),
                new Tuple<string, string, string>("EFA", "R", "Ensoniq ASR-10 instrument file. autoFD CD"),
                new Tuple<string, string, string>("EFE", "R+W", "Ensoniq EPS instrument file. autoinstwaveFD CD"),
                new Tuple<string, string, string>("EFK", "R", "Ensoniq KT instrument file. auto"),
                new Tuple<string, string, string>("EFQ", "R", "Ensoniq SQ1/SQ2/KS32 instrument file. auto"),
                new Tuple<string, string, string>("EFS", "R", "Ensoniq SQ80 instrument file. auto"),
                new Tuple<string, string, string>("EFV", "R", "Ensoniq VFX-SD instrument file. auto"),
                new Tuple<string, string, string>("EMB", "R", "Everest embedded bank file. auto"),
                new Tuple<string, string, string>("EMD", "R", "ABT extended modules. auto"),
                new Tuple<string, string, string>("EMY", "R+W", "EMelody Ericsson mobile ring-tone format. automidi"),
                new Tuple<string, string, string>("ESPS", "R", "ESPS audio file. auto"),
                new Tuple<string, string, string>("EUI", "R", "Ensoniq EPS family compacted disk image."),
                new Tuple<string, string, string>("EXS", "R+W", "Logic EXS24 instrument. autoinst"),
                new Tuple<string, string, string>("F2R", "R", "Farandoyle linear module format. auto"),
                new Tuple<string, string, string>("F3R", "R", "Farandoyle blocked linear module format. auto"),
                new Tuple<string, string, string>("F32", "R+W", "Floating point raw 32-bit IEEE data."),
                new Tuple<string, string, string>("F64", "R+W", "Floating point raw 64-bit IEEE data."),
                new Tuple<string, string, string>("FAR", "R", "Farandoyle module. auto"),
                new Tuple<string, string, string>("FFF", "R+W", "Gravis UltraSound PnP bank file. autocollinst"),
                new Tuple<string, string, string>("FLAC", "R+W", "Free lossless audio codec file. autowave*"),
                new Tuple<string, string, string>("FNK", "R", "FunkTracker modules. auto"),
                new Tuple<string, string, string>("FSB", "R", "FMOD SoundSystem sound bank. auto"),
                new Tuple<string, string, string>("FSM", "R", "Farandoyle sample format. auto"),
                new Tuple<string, string, string>("FZB", "R+W", "Casio FZ-1 bank dump format. autoinst"),
                new Tuple<string, string, string>("FZF", "R+W", "Casio FZ-1 full dump format. autocoll"),
                new Tuple<string, string, string>("FZV", "R+W", "Casio FZ-1 voice dump format. autowave"),
                new Tuple<string, string, string>("G721", "R+W", "G.721 4-bit (32 kbps) ADPCM format data. wave"),
                new Tuple<string, string, string>("G723", "R+W", "G.723 3/5-bit ADPCM format data. wave"),
                new Tuple<string, string, string>("G723-3", "R+W", "G.723 3-bit (24 kbps) ADPCM format data. wave"),
                new Tuple<string, string, string>("G723-5", "R+W", "G.723 5-bit (40 kbps) ADPCM format data. wave"),
                new Tuple<string, string, string>("G726", "R+W", "G.726 2/3/4/5-bit ADPCM format data. wave"),
                new Tuple<string, string, string>("G726-2", "R+W", "G.726 2-bit (16 kbps) ADPCM format data. wave"),
                new Tuple<string, string, string>("G726-3", "R+W", "G.726 3-bit (24 kbps) ADPCM format data. wave"),
                new Tuple<string, string, string>("G726-4", "R+W", "G.726 4-bit (32 kbps) ADPCM format data. wave"),
                new Tuple<string, string, string>("G726-5", "R+W", "G.726 5-bit (40 kbps) ADPCM format data. wave"),
                new Tuple<string, string, string>("GDM", "R", "Bells, Whistles, Sound Boards modules. auto"),
                new Tuple<string, string, string>("GI!", "R",
                    "GigaStudio/GigaSampler file - Split over multiple CD-ROM's. auto"),
                new Tuple<string, string, string>("GIG", "R+W", "GigaStudio/GigaSampler file - Normal. autocollinstwave / GigaStudio/GigaSampler file - Compressed. autocollinstwave"),
                new Tuple<string, string, string>("GKH", "R", "Ensoniq EPS family disk image file. autoFD CD"),
                new Tuple<string, string, string>("GSM", "R+W", "Raw GSM 06.10 audio streasm. wave / Raw 'byte aligned' GSM 06.10 audio stream. wave / US Robotics voice modems GSM w.o. header (VoiceGuide, RapidComm). autowave / US Robotics voice modems GSM w. header (QuickLink). autowave"),
                new Tuple<string, string, string>("HCOM", "R", "Sound Tools HCOM format. auto"),
                new Tuple<string, string, string>("HD", "R", "Sony PS2 SCEI instrument set. auto"),
                new Tuple<string, string, string>("IBK", "R", "Creative Labs FM bank. autoS"),
                new Tuple<string, string, string>("IFF", "R+W", "Interchange file format. autowave"),
                new Tuple<string, string, string>("IMF", "R", "id Software Music Format (560 Hz). auto"),
                new Tuple<string, string, string>("IMY", "R+W", "iMelody mobile phone ring-tone format. automidi"),
                new Tuple<string, string, string>("INI", "R+W / R", "Gravis UltraSound.ini bank setup. autocoll / IBM MWave DSP synthesizer bank setup (MWSYNTH.INI). auto"),
                new Tuple<string, string, string>("INRS", "R", "INRS-Telecommunications audio file."),
                new Tuple<string, string, string>("INS", "R+W / W / R / R", "Sample Cell II Mac instrument. autoinst / Cakewalk instrument definition file. coll / Ad Lib instrument. autoS / Ensoniq EPS family instrument file. auto"),
                new Tuple<string, string, string>("IST", "R", "Digitrakker instrument. auto"),
                new Tuple<string, string, string>("IT", "R", "Impulse Tracker modules. auto"),
                new Tuple<string, string, string>("ITI", "R+W", "Impulse Tracker instrument. autoinst"),
                new Tuple<string, string, string>("ITS", "R+W", "Impulse Tracker sample. autowave"),
                new Tuple<string, string, string>("K25", "R", "Kurzweil K2500 file. autoCD"),
                new Tuple<string, string, string>("K26", "R", "Kurzweil K2600 file. autoCD"),
                new Tuple<string, string, string>("KAWAI12", "R+W", "KAWAI R50/R50E/R50III/R100 ROM-dump. autowave"),
                new Tuple<string, string, string>("KDT", "R", "Konami KDT1 song. auto"),
                new Tuple<string, string, string>("KFT", "R", "Korg T-series waves. auto"),
                new Tuple<string, string, string>("KIT", "R+W",
                    "Native instrument Batter v1 drum-kit file. autocollinst"),
                new Tuple<string, string, string>("KMP", "R+W", "Korg Triton keymap file. autoinst / M3 keymap file. auto / Kronos keymap file. auto"),
                new Tuple<string, string, string>("KR1", "R+W",
                    "Kurzweil K2000/K2500/K2600 split file. autocollinstwavemidiCD"),
                new Tuple<string, string, string>("KRZ", "R+W",
                    "Kurzweil K2000 file (also K2500 & K2600). autocollinstwavemidiCD"),
                new Tuple<string, string, string>("KSC", "R+W", "Korg Triton/Trinity  script file. autocoll / M3 script file. autocoll / Kronos script file. autocoll"),
                new Tuple<string, string, string>("KSF", "R+W", "Korg Triton sample file. autowave"),
                new Tuple<string, string, string>("KSF", "R+W", "Korg Trinity sample file. autowave"),
                new Tuple<string, string, string>("KSF", "R+W", "Korg M3 sample file. auto"),
                new Tuple<string, string, string>("KSF", "R+W", "Korg Kronos sample file. auto"),
                new Tuple<string, string, string>("M4A", "R+W", "iTunes MPEG-4 audio format. autowave*"),
                new Tuple<string, string, string>("M4R", "R+W", "iPhone ring-tone. autowave*"),
                new Tuple<string, string, string>("MAP", "R+W",
                    "Native instrument Reaktor wavetable file - Embedded waves. autoinst"),
                new Tuple<string, string, string>("MAP", "R+W",
                    "Native instrument Reaktor wavetable file - Linked waves. autoinst"),
                new Tuple<string, string, string>("MAT", "R+W", "Matlab variables binary file. autocollwave"),
                new Tuple<string, string, string>("MAUD", "R", "MAUD sample format. auto"),
                new Tuple<string, string, string>("MDL", "R", "Digitrakker modules. auto"),
                new Tuple<string, string, string>("MED", "R", "OctaMED modules. auto"),
                new Tuple<string, string, string>("MID", "R+W / R", "Standard MIDI file. automidi / Roland D-50/MT-32 dump. autoS / Yamaha DX voice SysEx dump. autoS"),
                new Tuple<string, string, string>("MKA", "R", "Matroska audio file. auto"),
                new Tuple<string, string, string>("MKV", "R", "Matroska video file. auto"),
                new Tuple<string, string, string>("MLD", "R",
                    "MFi/MFi2 songs a.k.a. i-Melody a.k.a. Melody Format for i-Mode. auto"),
                new Tuple<string, string, string>("MLS", "R", "Miles Sound System compressed DLS file. auto"),
                new Tuple<string, string, string>("MMF", "R",
                    "SMAF songs, 'Synthetic Music Mobile Application Format'. autoS"),
                new Tuple<string, string, string>("MOD", "R", "Module file. auto"),
                new Tuple<string, string, string>("MOV", "R", "Apple QuickTime movie format. auto*"),
                new Tuple<string, string, string>("MP1", "R", "MPEG audio stream, layer I."),
                new Tuple<string, string, string>("MP2", "R+W", "MPEG audio stream, layer II. wave*"),
                new Tuple<string, string, string>("MP3", "R+W", "MPEG audio stream, layer III. wave*"),
                new Tuple<string, string, string>("MP4", "R+W", "MPEG-4 base media file format. autowave*"),
                new Tuple<string, string, string>("MPA", "R", "MPEG audio stream, layer I, II, 'II½' or III."),
                new Tuple<string, string, string>("MPC", "R+W", "Musepack audio compression. autowave"),
                new Tuple<string, string, string>("MPEG", "R", "MPEG system stream, audio+video. auto"),
                new Tuple<string, string, string>("MPG", "R", "MPEG system stream, audio+video. auto"),
                new Tuple<string, string, string>("MSS", "R+W", "Miles Sound System DLS 1 + XMI file. automidi"),
                new Tuple<string, string, string>("MT2", "R", "MadTracker 2 modules. auto"),
                new Tuple<string, string, string>("MTI", "R+W", "MadTracker 2 instrument. autoinst"),
                new Tuple<string, string, string>("MTM", "R", "MultiTracker modules. auto"),
                new Tuple<string, string, string>("MUS", "R+W / R", "Musifile MPEG Layer II audio stream. auto / Doom/Heretic music file. auto"),
                new Tuple<string, string, string>("MUS10", "R", "Mus10 audio file. auto"),
                new Tuple<string, string, string>("MWS", "W", "IBM MWave DSP synth instrument extracts. instwave"),
                new Tuple<string, string, string>("NIST", "R+W", "NIST Sphere file. auto"),
                new Tuple<string, string, string>("NKI", "R", "Native instrument Kontakt instrument. auto"),
                new Tuple<string, string, string>("NKM", "R", "Native instrument Kontakt multi bank. auto"),
                new Tuple<string, string, string>("NVF", "R+W", "Creative Labs Nomad voice file. autowave"),
                new Tuple<string, string, string>("O01", "R", "Typhoon voice file. auto"),
                new Tuple<string, string, string>("OGG", "R+W", "Vorbis Ogg stream. autowave*"),
                new Tuple<string, string, string>("OKT", "R", "Oktalyzer modules. auto"),
                new Tuple<string, string, string>("OPUS", "R", "Opus audio stream. auto*"),
                new Tuple<string, string, string>("OSP", "R+W", "Orion Sampler programs. autoinst"),
                new Tuple<string, string, string>("OUT", "R","Roland S-5xx series floppy image (S-50,S-51,S-330,W-30,S-500,S-550). autoCD FD"),
                new Tuple<string, string, string>("P", "R+W / R", "AKAI  programs. autoinstCD"),
                new Tuple<string, string, string>("PAC", "R", "SB Studio II package file. auto"),
                new Tuple<string, string, string>("PAF", "R+W", "Ensoniq PARIS audio file. autowave"),
                new Tuple<string, string, string>("PAT", "R+W", "Gravis Ultrasound GF1 patch file. autoinstwave"),
                new Tuple<string, string, string>("PBF", "R+W", "Turtle Beach Pinnacle bank file. autocollinst"),
                new Tuple<string, string, string>("PCG", "R+W", "Korg Trinity/Triton/M3/M50/Kronos bank file. autocollinst"),
                new Tuple<string, string, string>("PCM", "R", "OKI MSM6376 synth chip PCM format. auto"),
                new Tuple<string, string, string>("PGM", "R+W", "AKAI MPC-1000 drum set file + .WAV file. autoinstwave"),
                new Tuple<string, string, string>("PGM", "R+W","AKAI drum set file + .WAV file +  .SND file. autoinstwave"),
                new Tuple<string, string, string>("PLM", "R", "DisorderTracker2 modules. auto"),
                new Tuple<string, string, string>("PLS", "R", "DisorderTracker2 sample. auto"),
                new Tuple<string, string, string>("PPF", "R", "Turtle Beach Pinnacle program file. auto"),
                new Tuple<string, string, string>("PRG", "R+W", "WAVmaker program. autoinst"),
                new Tuple<string, string, string>("PSB", "R", "Turtle Beach Pinnacle sound bank. auto"),
                new Tuple<string, string, string>("PSION", "R", "PSION a-law file. auto"),
                new Tuple<string, string, string>("PSM", "R", "Protracker Studio modules. auto"),
                new Tuple<string, string, string>("PTM", "R", "Poly Tracker modules. auto"),
                new Tuple<string, string, string>("RA", "W", "RealAudio file. wave*"),
                new Tuple<string, string, string>("RAW", "R", "Signed 8-bit PCM data. wave / Rdos Raw OPL capture format. auto"),
                new Tuple<string, string, string>("RIF", "R", "Rockwell ADPCM format (Hotfax/Quicklink). auto"),
                new Tuple<string, string, string>("RMI", "R+W", "RIFF-MIDI file. automidicoll"),
                new Tuple<string, string, string>("ROCKWELL", "R+W", "Rockwell 2/3/4-bit ADPCM data. wave"),
                new Tuple<string, string, string>("ROCKWELL-2", "R+W", "Rockwell 2-bit ADPCM data. wave"),
                new Tuple<string, string, string>("ROCKWELL-3", "R+W", "Rockwell 3-bit ADPCM data. wave"),
                new Tuple<string, string, string>("ROCKWELL-4", "R+W", "Rockwell 4-bit ADPCM data. wave"),
                new Tuple<string, string, string>("ROL", "R", "AdLib Visual Composer songs. auto"),
                new Tuple<string, string, string>("ROM", "R",
                    "Roland MT-32 / CM32L / LAPC-1 control and PCM ROM dump. autoS"),
                new Tuple<string, string, string>("S", "R+W", "AKAI  sample. autowave"),
                new Tuple<string, string, string>("S1A", "R", "Yamaha EX5 'all' format. auto"),
                new Tuple<string, string, string>("S1M", "R+W", "Yamaha EX5 'waveforms' format. autocollinstwave"),
                new Tuple<string, string, string>("S1V", "R+W", "Yamaha EX5 'voices' format. autocollinstwave"),
                new Tuple<string, string, string>("S3I", "R+W", "ScreamTracker v3 instrument. autowave"),
                new Tuple<string, string, string>("S3M", "R", "ScreamTracker v3 modules. auto"),
                new Tuple<string, string, string>("S3P", "R+W", "AKAI MESA II/PC S-series program. autoinst"),
                new Tuple<string, string, string>("SAM", "R", "Signed 8-bit PCM file. wave"),
                new Tuple<string, string, string>("SB", "R+W", "Signed byte (8-bit) data. wave"),
                new Tuple<string, string, string>("SBI", "R", "Creative Labs FM Instrument. autoS"),
                new Tuple<string, string, string>("SBK", "R+W", "EMU SoundFont v1.x bank. autocollinstwave"),
                new Tuple<string, string, string>("SC2", "R+W", "Sample Cell II PC/Mac instrument. autoinst"),
                new Tuple<string, string, string>("SD", "R+W", "Sound Designer I file. autowave"),
                new Tuple<string, string, string>("SD2", "R+W", "Sound Designer II flattened file/data forks. autowave"),
                new Tuple<string, string, string>("SDK", "R",
                    "Roland S-5xx series floppy disk image . autoCD FD"),
                new Tuple<string, string, string>("SDS", "R+W / R", "MIDI Sample Dump Standard dump file. autowave / SmartSound SDS file."),
                new Tuple<string, string, string>("SDW", "R", "Signed dword (32-bit) data."),
                new Tuple<string, string, string>("SDX", "R", "MIDI Sample Dump Standard dump compacted by SDX. auto"),
                new Tuple<string, string, string>("SEQ", "R+W", "Sony Playstation MIDI sequences. automidi"),
                new Tuple<string, string, string>("SF", "R+W", "IRCAM / MTU SoundFile format. autowave"),
                new Tuple<string, string, string>("SF2", "R+W", "EMU SoundFont v2.x bank. autocollinstwave"),
                new Tuple<string, string, string>("SF2PACK", "R", "MIDI Converter Studio packed Sound Font. auto"),
                new Tuple<string, string, string>("SFARK", "R", "Melody Machine Compressed SoundFonts. *"),
                new Tuple<string, string, string>("SFD", "R", "SoundStage sound data file."),
                new Tuple<string, string, string>("SFI", "R", "SoundStage sound info file. auto"),
                new Tuple<string, string, string>("SFR", "R", "Sonic Foundry sample resource file. auto"),
                new Tuple<string, string, string>("SFZ", "R+W", "rgc:audio SFZ v1 instrument. autoinstwave"),
                new Tuple<string, string, string>("SFZ", "R", "Cakewalk SFZ v2 instrument. auto"),
                new Tuple<string, string, string>("SHN", "R+W", "Shorten lossless compression. autowave"),
                new Tuple<string, string, string>("SMD", "R", "J-Phone / SmdEd mobile songs. auto"),
                new Tuple<string, string, string>("SMP", "R", "Samplevision file. auto / Ad Lib Gold sample. auto / Avalon sample. autowave"),
                new Tuple<string, string, string>("SND", "R+W", "Unsigned 8-bit PCM data (W as .UB). wave / AKAI MPC-60/2000/2000XL/3000 sample file. autowave"),
                new Tuple<string, string, string>("SNDR", "R", "Sounder sound file. auto"),
                new Tuple<string, string, string>("SNDT", "R", "SndTools sound file. auto"),
                new Tuple<string, string, string>("SOU", "R", "SB Studio II sound file. auto"),
                new Tuple<string, string, string>("SPD", "R", "Speech data file."),
                new Tuple<string, string, string>("SPL", "R", "Digitrakker sample. auto"),
                new Tuple<string, string, string>("SPPACK", "R+W", "SPPack sound sample. autowave"),
                new Tuple<string, string, string>("SQ", "R", "Sony PS2 SCEI sequence. auto"),
                new Tuple<string, string, string>("STM", "R", "ScreamTracker v2 modules."),
                new Tuple<string, string, string>("STS", "R+W", "Creamware STS-series sampler programs. autocoll"),
                new Tuple<string, string, string>("SVQ", "R", "Roland sequencer file. auto"),
                new Tuple<string, string, string>("SVX", "R", "Interchange file format. auto"),
                new Tuple<string, string, string>("SW", "R+W", "Signed word (16-bit) data. wave"),
                new Tuple<string, string, string>("SXT", "R+W", "Propellerheads Reason NN-XT format. autoinstwave"),
                new Tuple<string, string, string>("SYX", "R", "Roland D-50 patch SysEx dump. autoS"),
                new Tuple<string, string, string>("SYX", "R", "Roland MT-32 (and compatibles) timbre SysEx dump. autoS"),
                new Tuple<string, string, string>("SYX", "R", "Yamaha DX7 voice SysEx dump. autoS"),
                new Tuple<string, string, string>("SYX", "R", "Yamaha DX7s / DX7II / DX200 voice SysEx dump. autoS"),
                new Tuple<string, string, string>("SYX", "R", "Yamaha DX21 / DX27 / DX100 voice SysEx dump. autoS"),
                new Tuple<string, string, string>("SYX", "R", "Yamaha DX11 / TX81z voice SysEx dump. autoS"),
                new Tuple<string, string, string>("SYW", "R+W", "Yamaha SY-series wave file. autowave"),
                new Tuple<string, string, string>("TVD", "R", "Yamaha Tyros 2 custom drum voice file. instauto"),
                new Tuple<string, string, string>("TVN", "R+W", "Yamaha Tyros 2 custom voice file. instauto"),
                new Tuple<string, string, string>("TXT", "W", "Ascii text parameter descriptions. autocollinstwave"),
                new Tuple<string, string, string>("TXT", "R+W", "Ascii text formatted audio data. autowave"),
                new Tuple<string, string, string>("TXT", "R+W", "RTTTL / Nokring mobile ring-tone format. automidi"),
                new Tuple<string, string, string>("TXT", "R+W", "Steinberg LM-4 bank. autocollinst"),
                new Tuple<string, string, string>("TXW", "R+W", "Yamaha TX16W wave file. autowave"),
                new Tuple<string, string, string>("U255LAW", "R+W", "Exponential 8-bit format. wave"),
                new Tuple<string, string, string>("UAX", "R", "Unreal Tournament audio packages. auto"),
                new Tuple<string, string, string>("UB", "R+W", "Unsigned byte (8-bit) data. wave"),
                new Tuple<string, string, string>("UDW", "R", "Unsigned dword (32-bit) data."),
                new Tuple<string, string, string>("UMX", "R", "Unreal Tournament music package. auto"),
                new Tuple<string, string, string>("ULAW", "R+W", "G.711 mu-law US telephony format. wave"),
                new Tuple<string, string, string>("ULT", "R", "UltraTracker modules. auto"),
                new Tuple<string, string, string>("ULW", "R+W", "G.711 mu-law US telephony format. wave"),
                new Tuple<string, string, string>("UNI", "R", "MikMod 'UniMod' format. auto"),
                new Tuple<string, string, string>("UVD", "R+W", "Yamaha Tyros 3 custom drum voice file. autoinst"),
                new Tuple<string, string, string>("UVN", "R+W", "Yamaha Tyros 3 custom voice file. autoinst"),
                new Tuple<string, string, string>("UW", "R+W", "Unsigned word (16-bit) data. wave"),
                new Tuple<string, string, string>("UWF", "R", "UltraTracker wave file. auto"),
                new Tuple<string, string, string>("V8", "R", "Covox 8-bit audio. autowave"),
                new Tuple<string, string, string>("VAB", "R+W", "Sony Playstation / PS2 bank file. autocollinst"),
                new Tuple<string, string, string>("VAG", "R+W", "Sony Playstation / PS2 compressed sound file. autowave"),
                new Tuple<string, string, string>("VAP", "R+W", "Annotated speech file. collwave"),
                new Tuple<string, string, string>("VM1", "R", "Panasonic voice file."),
                new Tuple<string, string, string>("VOC", "R+W", "Creative Labs 'newer style' sound file. autowave"),
                new Tuple<string, string, string>("VOC", "R+W", "Creative Labs 'older style' sound file. autowave"),
                new Tuple<string, string, string>("VOX", "R+W", "Dialogic 4-bit ADPCM file. autowave"),
                new Tuple<string, string, string>("VOX", "R", "Talking Technology Incorporated file. auto"),
                new Tuple<string, string, string>("VOX-6K", "R+W", "Dialogic 4-bit ADPCM file (6000 Hz). autowave"),
                new Tuple<string, string, string>("VOX-8K", "R+W", "Dialogic 4-bit ADPCM file (8000 Hz). autowave"),
                new Tuple<string, string, string>("VSB", "R+W", "Virtual Sampler Bank file. autocollinstwave"),
                new Tuple<string, string, string>("W??", "R+W", "Yamaha TX16W wave file. autowave"),
                new Tuple<string, string, string>("W??", "R+W", "Yamaha SY-series wave file. autowave"),
                new Tuple<string, string, string>("W2A", "R", "Yamaha Motif 'all' format. auto"),
                new Tuple<string, string, string>("W2V", "R+W", "Yamaha Motif 'voices' format. autocollinstwave"),
                new Tuple<string, string, string>("W2W", "R+W", "Yamaha Motif 'waveforms' format. autocollinstwave"),
                new Tuple<string, string, string>("W4KSND", "R+W", "Wusik 4000 instrument. autoinstwave"),
                new Tuple<string, string, string>("W7A", "R", "Yamaha Motif ES 'all' format. auto"),
                new Tuple<string, string, string>("W7V", "R+W", "Yamaha Motif ES 'voices' format. autocollinstwave"),
                new Tuple<string, string, string>("W7W", "R+W", "Yamaha Motif ES 'waveforms' format. autocollinstwave"),
                new Tuple<string, string, string>("W64", "R+W", "Sonic Foundry Wave-64 format. autowave"),
                new Tuple<string, string, string>("WAV", "R+W", "Microsoft wave format. autowave"),
                new Tuple<string, string, string>("WAV", "R", "Broadcast wave format (EBU BWF). autowave"),
                new Tuple<string, string, string>("WA!", "R+W", "GigaStudio/GigaSampler compressed wave file. autowave"),
                new Tuple<string, string, string>("WFB", "R+W", "Turtle Beach WaveFront bank format. autocoll"),
                new Tuple<string, string, string>("WFD", "R+W", "Turtle Beach WaveFront drum set format. autoinst"),
                new Tuple<string, string, string>("WFP", "R+W", "Turtle Beach WaveFront program format. autoinstwave"),
                new Tuple<string, string, string>("WLF", "R", "id Software Music Format (700 Hz). auto"),
                new Tuple<string, string, string>("WMA", "R+W", "Windows Media Audio autowave*"),
                new Tuple<string, string, string>("WMV", "R", "Windows Media Video file. auto"),
                new Tuple<string, string, string>("WRF", "R+W", "Westacott WinRanX instrument file. autoinst"),
                new Tuple<string, string, string>("WRK", "R", "CakeWalk work file. auto"),
                new Tuple<string, string, string>("WUSIKSND", "R+W", "Wusikstation sound file. autoinstwave"),
                new Tuple<string, string, string>("WUSIKPACK", "R", "Wusikstation pack file. autoinstwave"),
                new Tuple<string, string, string>("WV", "R+W", "WavPack losslessly compressed file. autowave"),
                new Tuple<string, string, string>("X0A", "R", "Yamaha Motif XS 'all' format. auto"),
                new Tuple<string, string, string>("X0V", "R+W", "Yamaha Motif XS 'voices' format. auto"),
                new Tuple<string, string, string>("X0W", "R+W", "Yamaha Motif XS 'waveforms' format. auto"),
                new Tuple<string, string, string>("X3A", "R", "Yamaha Motif XF 'all' format. auto"),
                new Tuple<string, string, string>("X3V", "R+W", "Yamaha Motif XF 'voices' format. auto"),
                new Tuple<string, string, string>("X3W", "R+W", "Yamaha Motif XF 'waveforms' format. auto"),
                new Tuple<string, string, string>("XI", "R+W", "FastTracker 2 instrument. autoinstwave"),
                new Tuple<string, string, string>("XM", "R", "FastTracker 2 extended modules. auto"),
                new Tuple<string, string, string>("XMI", "R+W", "Miles Sound System extended MIDI file. automidi"),
                new Tuple<string, string, string>("YADPCM", "R+W", "Raw Yamaha 4-bit ADPCM format data."),
                new Tuple<string, string, string>("???", "R", "Yamaha A3000 sample file. autowave")
            };










        }

        #endregion
    }


    #region Subclass Regel (Rule)

    /// <summary>
    /// To Save what should happen with the different Filetypes
    /// </summary>
    public class Regel
    {
        /// <summary>
        /// List of the Filetypes to use them when ordering
        /// </summary>
        private readonly List<string> _dateitypen;

        /// <summary>
        /// Path where the Files should be moved to
        /// </summary>
        public string Pfad  { get; }

        /// <summary>
        /// Returns the List of the Filetypes as a string to show them in List View
        /// </summary>
        public string DateitypenPublic => string.Join(";", _dateitypen.ToArray());

        /// <summary>
        /// Simple Constructor
        /// </summary>
        /// <param name="dateitypen"></param>
        /// <param name="pfad"></param>
        public Regel(List<string> dateitypen, string pfad)
        {
            _dateitypen = dateitypen;
            Pfad = pfad;
        }
    }

    #endregion
}
