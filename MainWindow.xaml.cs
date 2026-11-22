using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
namespace DJRussoFXHub;
public partial class MainWindow : Window
{
 private record PluginInfo(string id,string name,string category,string status);
 private sealed class PluginCard { public string Id {get;set;}=""; public bool IsSelected {get;set;} public bool IsReady {get;set;} public string ActionLabel {get;set;}=""; public string RemoveLabel {get;set;}=""; public Visibility ActionVisibility => IsReady ? Visibility.Visible : Visibility.Collapsed; public string FallbackName {get;set;}=""; public int LogoHeight {get;set;}=78; public BitmapSource? Logo {get;set;} public string Category {get;set;}=""; public string Footer {get;set;}=""; public Visibility MeterVisibility {get;set;}=Visibility.Collapsed; public bool MeterReady {get;set;} public bool MeterInstallReady {get;set;} public string MeterImportLabel {get;set;}=""; public string MeterLaunchLabel {get;set;}=""; public string MeterRemoveLabel {get;set;}=""; public string BadgeSymbol {get;set;}="✕"; public System.Windows.Media.Brush BadgeBrush {get;set;}=System.Windows.Media.Brushes.IndianRed; public string BadgeTip {get;set;}=""; }
 private record EffectPackage(string id,string detector,List<string> effectIds);
 private record EffectManifest(int schemaVersion,List<EffectPackage> packages);
 private EffectManifest manifest=new(1,new());
 private void LoadManifest(){try{var file=Path.Combine(AppContext.BaseDirectory,"effect-packages.json");var m=JsonSerializer.Deserialize<EffectManifest>(File.ReadAllText(file),new JsonSerializerOptions{PropertyNameCaseInsensitive=true});if(m?.schemaVersion==1 && m.packages!=null)manifest=m;}catch{}}
 private string selectedProductId="meter-x"; private List<PluginInfo> plugins=new(); private string lang="en"; private string theme="neon"; private bool ready;
 private readonly Dictionary<string,string[]> translations=new() {
  ["guide"]=["GUIDE / README","GUIDA / LEGGIMI","GUÍA / LÉEME","GUIDE / LISEZ-MOI","HILFE / LIESMICH"],

  ["shellTitle"]=["FX SHELL • 3 EFFECTS","FX SHELL • 3 EFFETTI","FX SHELL • 3 EFECTOS","FX SHELL • 3 EFFETS","FX SHELL • 3 EFFEKTE"],
  ["shellHint"]=["One VST3 file provides Sidechain X, Reverse FX and Stutter FX • bundled build v3.0.3","Un solo VST3 contiene Sidechain X, Reverse FX e Stutter FX • build inclusa v3.0.3","Un VST3 incluye Sidechain X, Reverse FX y Stutter FX • compilación v3.0.3","Un VST3 contient Sidechain X, Reverse FX et Stutter FX • version v3.0.3","Eine VST3-Datei enthält Sidechain X, Reverse FX und Stutter FX • Build v3.0.3"],
  ["shellInstall"]=["INSTALL FX SHELL","INSTALLA FX SHELL","INSTALAR FX SHELL","INSTALLER FX SHELL","FX SHELL INSTALLIEREN"],
  ["shellRemove"]=["UNINSTALL FX SHELL","DISINSTALLA FX SHELL","DESINSTALAR FX SHELL","DÉSINSTALLER FX SHELL","FX SHELL DEINSTALLIEREN"],
  ["shellMissing"]=["NOT INSTALLED IN STANDARD VST3 FOLDER","NON INSTALLATA NELLA CARTELLA VST3 STANDARD","NO INSTALADA EN LA CARPETA VST3 ESTÁNDAR","NON INSTALLÉE DANS LE DOSSIER VST3 STANDARD","NICHT IM STANDARD-VST3-ORDNER INSTALLIERT"],
  ["shellManaged"]=["FILE INSTALLED BY HUB • verify in FL Studio","FILE INSTALLATO DALL'HUB • verifica in FL Studio","ARCHIVO INSTALADO POR HUB • comprobar en FL Studio","FICHIER INSTALLÉ PAR HUB • vérifier dans FL Studio","DATEI VOM HUB INSTALLIERT • in FL Studio prüfen"],
  ["shellExternal"]=["FILE ALREADY PRESENT • not managed by HUB","FILE GIÀ PRESENTE • non gestito dall'HUB","ARCHIVO YA PRESENTE • no gestionado por HUB","FICHIER DÉJÀ PRÉSENT • non géré par HUB","DATEI SCHON VORHANDEN • nicht vom HUB verwaltet"],
  ["shellOther"]=["DIFFERENT PLUGIN AT DESTINATION • left unchanged","PLUGIN DIVERSO NELLA DESTINAZIONE • non modificato","OTRO PLUGIN EN EL DESTINO • sin cambios","AUTRE PLUGIN À DESTINATION • inchangé","ANDERES PLUGIN AM ZIEL • unverändert"],
  ["shellModified"]=["INSTALLED FILE HAS CHANGED • uninstall disabled","FILE INSTALLATO MODIFICATO • disinstallazione disattivata","ARCHIVO INSTALADO MODIFICADO • desinstalación desactivada","FICHIER INSTALLÉ MODIFIÉ • désinstallation désactivée","INSTALLIERTE DATEI VERÄNDERT • Deinstallation deaktiviert"],
  ["shellInstallConfirm"]=["Install ONE FX SHELL VST3 for all three effects?\nClose FL Studio first. Windows will request administrator permission.","Installare UN solo VST3 FX SHELL per tutti e tre gli effetti?\nChiudi prima FL Studio. Windows chiederà l'autorizzazione amministratore.","¿Instalar UN VST3 FX SHELL para los tres efectos?\nCierra FL Studio. Windows pedirá permisos de administrador.","Installer UN seul VST3 FX SHELL pour les trois effets ?\nFermez FL Studio. Windows demandera l'autorisation administrateur.","EINE FX SHELL VST3-Datei für alle drei Effekte installieren?\nFL Studio schließen. Windows fordert Administratorrechte."],
  ["shellRemoveConfirm"]=["Remove the HUB-managed FX SHELL file? ALL THREE effects will disappear after rescan. Close FL Studio first.","Rimuovere FX SHELL installata dall'HUB? TUTTI E TRE gli effetti spariranno dopo la scansione. Chiudi prima FL Studio.","¿Eliminar FX SHELL instalada por HUB? Los TRES efectos desaparecerán tras escanear. Cierra FL Studio.","Supprimer FX SHELL géré par HUB ? Les TROIS effets disparaîtront après analyse. Fermez FL Studio.","FX SHELL vom HUB entfernen? ALLE DREI Effekte verschwinden nach neuem Scan. FL Studio schließen."],
  ["shellInstalled"]=["FX SHELL file installed successfully. Open FL Studio > Plugin Manager > Find installed plugins + Verify plugins. Test all three effects.","File FX SHELL installato. Apri FL Studio > Gestione plugin > Cerca plugin installati + Verifica plugin. Prova tutti e tre gli effetti.","Archivo FX SHELL instalado. Abre FL Studio > Administrador de plugins > Buscar y verificar plugins. Prueba los tres efectos.","Fichier FX SHELL installé. Ouvrez le gestionnaire de plugins de FL Studio, lancez une recherche et testez les trois effets.","FX SHELL installiert. In FL Studio Plugin-Manager neu scannen und alle drei Effekte testen."],
  ["shellRemoved"]=["FX SHELL file removed. Rescan plugins in FL Studio; all three effects should disappear.","File FX SHELL rimosso. Ripeti la scansione in FL Studio: i tre effetti dovrebbero scomparire.","FX SHELL eliminada. Vuelve a escanear en FL Studio: los tres efectos deberían desaparecer.","FX SHELL supprimée. Relancez le scan FL Studio : les trois effets devraient disparaître.","FX SHELL entfernt. FL Studio erneut scannen: die drei Effekte sollten verschwinden."],
  ["payloadBad"]=["Packaged FX SHELL binary missing or SHA-256/PE x64 verification failed.","Binario FX SHELL assente o verifica SHA-256/PE x64 non superata.","Binario FX SHELL ausente o fallo de verificación SHA-256/PE x64.","Binaire FX SHELL absent ou vérification SHA-256/PE x64 échouée.","FX SHELL-Binärdatei fehlt oder SHA-256/PE x64-Prüfung fehlgeschlagen."],
  ["shellConflict"]=["A VST3 with this name already exists. HUB will not overwrite it; back it up and inspect it manually.","Esiste già un VST3 con questo nome. L'HUB non lo sovrascrive: verifica e salva una copia manualmente.","Ya existe un VST3 con este nombre. HUB no lo sobrescribirá.","Un VST3 de ce nom existe déjà. HUB ne l'écrasera pas.","Eine VST3-Datei dieses Namens existiert bereits. HUB überschreibt sie nicht."],
  ["shellChanged"]=["File changed or not owned by this HUB. It was not deleted.","File modificato o non gestito da questo HUB. Non è stato eliminato.","Archivo modificado o no gestionado por HUB. No se eliminó.","Fichier modifié ou non géré par HUB. Aucune suppression.","Datei geändert oder nicht vom HUB verwaltet. Nicht gelöscht."],
  ["notManaged"]=["No unmodified HUB-managed FX SHELL installation found; no file removed.","Nessuna FX SHELL integra installata dall'HUB: nessun file eliminato.","No se encontró una FX SHELL gestionada e intacta; no se eliminó nada.","Pas de FX SHELL intacte gérée par HUB ; aucun fichier supprimé.","Keine unveränderte HUB-Installation gefunden; nichts entfernt."],
  ["permissionError"]=["Windows access denied. Close FL Studio and retry with administrator permission.","Accesso Windows negato. Chiudi FL Studio e riprova autorizzando l'amministratore.","Acceso denegado. Cierra FL Studio y acepta los permisos de administrador.","Accès refusé. Fermez FL Studio et autorisez l'administrateur.","Zugriff verweigert. FL Studio schließen und Administratorrechte gewähren."],
  ["ioError"]=["Windows file operation failed. Close FL Studio, check destination and retry.","Operazione file Windows non riuscita. Chiudi FL Studio, controlla la destinazione e riprova.","Error en operación de archivo. Cierra FL Studio y revisa la ruta.","Échec de l'opération sur le fichier. Fermez FL Studio et vérifiez le chemin.","Dateioperation fehlgeschlagen. FL Studio schließen, Ziel prüfen."],
  ["cancelled"]=["Windows administrator prompt canceled. No change made.","Richiesta amministratore annullata. Nessuna modifica effettuata.","Permiso de administrador cancelado. Sin cambios.","Autorisation administrateur annulée. Aucun changement.","Administratorabfrage abgebrochen. Keine Änderung."],
  ["unknownError"]=["Install/uninstall failed. The current installation was not intentionally changed.","Installazione/disinstallazione non riuscita. Nessuna modifica intenzionale ai file esistenti.","Instalación/desinstalación fallida. No se modificaron archivos existentes intencionalmente.","Installation/désinstallation échouée. Aucun fichier existant modifié volontairement.","Installation/Deinstallation fehlgeschlagen. Bestehende Dateien wurden nicht absichtlich geändert."],
  ["receiptFailed"]=["The file was installed but HUB could not save its ownership record. Uninstall from HUB is disabled until this is resolved.","Il file è installato ma l'HUB non ha potuto salvare la registrazione. Disinstallazione dall'HUB disattivata finché non viene risolto.","Archivo instalado pero no se pudo guardar el registro de propiedad.","Fichier installé mais impossible d'enregistrer sa gestion.","Datei installiert, aber HUB konnte den Besitznachweis nicht speichern."],

  ["searchProducts"]=["SEARCH PRODUCTS","CERCA PRODOTTI","BUSCAR PRODUCTOS","RECHERCHER DES PRODUITS","PRODUKTE SUCHEN"],
  ["filterProducts"]=["FILTER PRODUCTS","FILTRA PRODOTTI","FILTRAR PRODUCTOS","FILTRER LES PRODUITS","PRODUKTE FILTERN"],
  ["searchTip"]=["Search by product name or category","Cerca per nome o categoria","Buscar por nombre o categoría","Rechercher par nom ou catégorie","Nach Name oder Kategorie suchen"],
  ["filterTip"]=["Show products by availability","Mostra i prodotti per disponibilità","Mostrar productos por disponibilidad","Afficher les produits selon leur disponibilité","Produkte nach Verfügbarkeit anzeigen"],
  ["diagnostics"]=["DIAGNOSTICS","DIAGNOSTICA","DIAGNÓSTICO","DIAGNOSTIC","DIAGNOSE"],
  ["backup"]=["CREATE BACKUP","CREA BACKUP","CREAR COPIA DE SEGURIDAD","CRÉER UNE SAUVEGARDE","BACKUP ERSTELLEN"],
  ["products"]=["Products","Prodotti","Productos","Produits","Produkte"],
  ["installedShort"]=["Installed","Installati","Instalados","Installés","Installiert"],
  ["comingSoonShort"]=["Coming soon","In arrivo","Próximamente","Prochainement","Demnächst"],
  ["theme"]=["Theme","Tema","Tema","Thème","Design"],
  ["neon"]=["Cyberpunk Neon","Cyberpunk Neon","Cyberpunk Neón","Cyberpunk Néon","Cyberpunk Neon"],
  ["dark"]=["Dark Professional","Scuro professionale","Oscuro profesional","Sombre professionnel","Dunkel professionell"],
  ["light"]=["Light Modern","Chiaro moderno","Claro moderno","Clair moderne","Hell modern"],
  ["install"]=["INSTALL LOCAL","INSTALLA LOCALE","INSTALAR LOCAL","INSTALLER LOCAL","LOKAL INSTALLIEREN"],
  ["remove"]=["UNINSTALL","DISINSTALLA","DESINSTALAR","DÉSINSTALLER","DEINSTALLIEREN"],
  ["choose"]=["Choose a verified VST3 package for this effect. A combined FX SHELL package cannot be assigned to three separate plugin IDs.","Seleziona un pacchetto VST3 verificato per questo effetto. Una FX SHELL unica non equivale a tre plugin distinti.","Selecciona un VST3 verificado para este efecto. FX SHELL no equivale a tres plugins.","Choisissez un VST3 vérifié pour cet effet. FX SHELL n'est pas trois plugins distincts.","Wähle ein geprüftes VST3 für diesen Effekt. FX SHELL ist nicht drei separate Plugins."],
  ["confirm"]=["Install or replace the selected VST3? Close your DAW first.","Installare o sostituire il VST3 selezionato? Chiudi prima la DAW.","¿Instalar o reemplazar el VST3? Cierra la DAW.","Installer ou remplacer le VST3 ? Fermez la DAW.","VST3 installieren oder ersetzen? DAW zuerst schließen."],
  ["confirmremove"]=["Remove the managed plugin installation? Close your DAW first.","Rimuovere il plugin installato tramite HUB? Chiudi prima la DAW.","¿Eliminar el plugin gestionado? Cierra la DAW.","Supprimer le plugin géré ? Fermez la DAW.","Verwaltetes Plugin entfernen? DAW zuerst schließen."],
  ["noinstall"]=["No HUB-managed installation was found.","Nessuna installazione gestita dall'HUB trovata.","No hay instalación gestionada por HUB.","Aucune installation gérée par HUB.","Keine HUB-verwaltete Installation gefunden."],
  ["completed"]=["Operation completed. Restart your DAW to rescan plugins.","Operazione completata. Riavvia la DAW per rilevare i plugin.","Operación completada. Reinicia la DAW.","Opération terminée. Redémarrez la DAW.","Fertig. DAW für Plugin-Scan neu starten."],
  ["settings"]=["SETTINGS","IMPOSTAZIONI","AJUSTES","PARAMÈTRES","EINSTELLUNGEN"],
  ["settingshint"]=["Interface language","Lingua dell'interfaccia","Idioma de la interfaz","Langue de l’interface","Sprache der Oberfläche"],
  ["source"]=["Update source: official GitHub Releases","Fonte aggiornamenti: GitHub Releases ufficiali","Fuente: GitHub Releases (sin configurar)","Source : GitHub Releases (non configurée)","Quelle: GitHub Releases (nicht konfiguriert)"],
  ["shellfound"]=["FX SHELL detected; individual effects not verified","FX SHELL rilevato; singoli effetti non verificati","FX SHELL detectado; efectos individuales sin verificar","FX SHELL détecté ; effets individuels non vérifiés","FX SHELL erkannt; einzelne Effekte nicht geprüft"],
  ["subtitle"]=["14 PLUGINS • ONE CREATIVE ECOSYSTEM","14 PLUGIN • UN UNICO ECOSISTEMA CREATIVO","14 PLUGINS • UN ECOSISTEMA CREATIVO","14 PLUGINS • UN ÉCOSYSTÈME CRÉATIF","14 PLUGINS • EIN KREATIVES ÖKOSYSTEM"],
  ["updates"]=["CHECK UPDATES","CONTROLLA AGGIORNAMENTI","BUSCAR ACTUALIZACIONES","VÉRIFIER LES MISES À JOUR","UPDATES PRÜFEN"],
  ["folder"]=["OPEN VST3 FOLDER","APRI CARTELLA VST3","ABRIR CARPETA VST3","OUVRIR DOSSIER VST3","VST3-ORDNER ÖFFNEN"],
  ["scan"]=["SCAN INSTALLED SOFTWARE","RILEVA SOFTWARE INSTALLATI","BUSCAR SOFTWARE INSTALADO","DÉTECTER LES LOGICIELS INSTALLÉS","INSTALLIERTE SOFTWARE SUCHEN"],
  ["language"]=["Language","Lingua","Idioma","Langue","Sprache"],
  ["ready"]=["Ready • Local FX SHELL install available; GitHub release check enabled.","Pronto • Installazione locale FX SHELL disponibile; controllo aggiornamenti GitHub attivo.","Listo • Instalación local de FX SHELL disponible; GitHub pendiente.","Prêt • Installation locale de FX SHELL disponible ; GitHub à configurer.","Bereit • Lokale FX SHELL-Installation verfügbar; GitHub noch nicht eingerichtet."],
  ["planned"]=["COMING SOON","IN ARRIVO","PRÓXIMAMENTE","PROCHAINEMENT","DEMNÄCHST"],
  ["available"]=["READY FOR TESTING","PRONTO PER I TEST","LISTO PARA PRUEBAS","PRÊT POUR LES TESTS","TESTBEREIT"],
  ["installed"]=["INSTALLED (FILE FOUND)","INSTALLATO (FILE TROVATO)","INSTALADO (ARCHIVO ENCONTRADO)","INSTALLÉ (FICHIER TROUVÉ)","INSTALLIERT (DATEI GEFUNDEN)"],
  ["notfound"]=["NOT DETECTED","NON RILEVATO","NO DETECTADO","NON DÉTECTÉ","NICHT GEFUNDEN"],
  ["unavailable"]=["Automatic updates are not configured. Verified release packages and a signed manifest are required.","Aggiornamenti automatici non configurati. Servono pacchetti verificati e un manifesto firmato.","Actualizaciones automáticas no configuradas. Se requieren paquetes verificados y un manifiesto firmado.","Mises à jour automatiques non configurées. Des paquets vérifiés et un manifeste signé sont requis.","Automatische Updates nicht eingerichtet. Verifizierte Pakete und ein signiertes Manifest sind erforderlich."],
  ["missingfolder"]=["VST3 folder not found: ","Cartella VST3 non trovata: ","Carpeta VST3 no encontrada: ","Dossier VST3 introuvable : ","VST3-Ordner nicht gefunden: "],
  ["dynamics"]=["Dynamics","Dinamica","Dinámica","Dynamique","Dynamik"],
  ["creative"]=["Creative FX","Effetti creativi","Efectos creativos","Effets créatifs","Kreative Effekte"],
  ["analysis"]=["Analysis","Analisi","Análisis","Analyse","Analyse"],
  ["mastering"]=["Mastering","Mastering","Masterización","Mastering","Mastering"],
  ["vocal"]=["Vocal","Voce","Voz","Voix","Gesang"],
  ["eq"]=["EQ","EQ","EQ","EQ","EQ"],
  ["spatial"]=["Spatial","Spaziale","Espacial","Spatial","Raumklang"],
  ["saturation"]=["Saturation","Saturazione","Saturación","Saturation","Sättigung"],
  ["ai"]=["AI Offline","IA offline","IA sin conexión","IA hors ligne","Offline-KI"],
  ["shellBlocked"]=["Installation/removal disabled: an existing different or unmanaged VST3 must be checked manually.","Installazione/rimozione disattivata: è presente un VST3 diverso o non gestito, da controllare manualmente.","Instalación/eliminación desactivada: hay un VST3 diferente o no gestionado.","Installation/suppression désactivée : un VST3 différent ou non géré est présent.","Installation/Entfernung deaktiviert: eine andere oder nicht verwaltete VST3-Datei ist vorhanden."],
  ["diagTitle"]=["Diagnostics","Diagnostica","Diagnóstico","Diagnostic","Diagnose"],
  ["diagVstFolder"]=["VST3 folder","Cartella VST3","Carpeta VST3","Dossier VST3","VST3-Ordner"],
  ["diagVstFound"]=["FOUND","TROVATA","ENCONTRADA","TROUVÉ","GEFUNDEN"],
  ["diagVstMissing"]=["NOT FOUND","NON TROVATA","NO ENCONTRADA","INTROUVABLE","NICHT GEFUNDEN"],
  ["diagMeterFound"]=["FOUND","PRESENTE","DETECTADO","DÉTECTÉ","GEFUNDEN"],
  ["diagMeterMissing"]=["NOT FOUND","MANCANTE","NO DETECTADO","INTROUVABLE","NICHT GEFUNDEN"],
  ["diagFlLocations"]=["Standard FL Studio paths","Percorsi standard FL Studio","Rutas estándar de FL Studio","Emplacements standard de FL Studio","FL Studio-Standardpfade"],
  ["diagFlFound"]=["FOUND","TROVATI","ENCONTRADAS","TROUVÉS","GEFUNDEN"],
  ["diagFlMissing"]=["NOT FOUND","NON TROVATI","NO ENCONTRADAS","INTROUVABLES","NICHT GEFUNDEN"],
  ["diagPackage"]=["Bundled FX SHELL","Pacchetto FX SHELL","Paquete FX SHELL","Paquet FX SHELL","FX SHELL-Paket"],
  ["diagPackageValid"]=["VALID","VALIDO","VÁLIDO","VALIDE","GÜLTIG"],
  ["diagPackageInvalid"]=["INVALID OR MISSING","NON VALIDO O MANCANTE","NO VÁLIDO O AUSENTE","INVALIDE OU MANQUANT","UNGÜLTIG ODER FEHLT"],
  ["diagShellMissing"]=["NOT INSTALLED","NON INSTALLATO","NO INSTALADO","NON INSTALLÉ","NICHT INSTALLIERT"],
  ["diagShellManaged"]=["INSTALLED BY HUB","INSTALLATO DALL'HUB","INSTALADO POR HUB","INSTALLÉ PAR LE HUB","VOM HUB INSTALLIERT"],
  ["diagShellExternal"]=["PRESENT (NOT MANAGED BY HUB)","PRESENTE (NON GESTITO DALL'HUB)","PRESENTE (NO GESTIONADO POR HUB)","PRÉSENT (NON GÉRÉ PAR LE HUB)","VORHANDEN (NICHT VOM HUB VERWALTET)"],
  ["diagShellConflict"]=["DIFFERENT VST3 FILE DETECTED","RILEVATO UN VST3 DIFFERENTE","SE DETECTÓ UN VST3 DIFERENTE","UN AUTRE FICHIER VST3 EST DÉTECTÉ","ANDERE VST3-DATEI GEFUNDEN"],
  ["diagShellModified"]=["HUB-INSTALLED FILE MODIFIED","FILE INSTALLATO DALL'HUB MODIFICATO","ARCHIVO DEL HUB MODIFICADO","FICHIER INSTALLÉ PAR LE HUB MODIFIÉ","VOM HUB INSTALLIERTE DATEI VERÄNDERT"],
  ["diagShellUnknown"]=["UNKNOWN STATUS","STATO SCONOSCIUTO","ESTADO DESCONOCIDO","ÉTAT INCONNU","STATUS UNBEKANNT"],
  ["diagReadOnly"]=["Read-only diagnostic. It cannot confirm that the plugins load correctly in FL Studio.","Diagnostica di sola lettura. Non può confermare il caricamento corretto dei plugin in FL Studio.","Diagnóstico de solo lectura. No confirma que los plugins se carguen correctamente en FL Studio.","Diagnostic en lecture seule. Il ne confirme pas le chargement correct des plugins dans FL Studio.","Diagnose ohne Änderungen. Das korrekte Laden der Plugins in FL Studio kann nicht bestätigt werden."],

 };
 private int Index => lang switch {"it"=>1,"es"=>2,"fr"=>3,"de"=>4,_=>0};
 private string T(string key)=>translations.TryGetValue(key,out var values)?values[Index]:key;
 private string ManagedPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DJRusso","FXHub","managed.json");
 private string VstDir=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),"VST3");
 private string PrefPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DJRusso","FXHub","settings.json");
 public MainWindow(){searchDebounce.Tick+=ApplySearchDebounce;Closed+=(_,_)=>searchDebounce.Stop();InitializeComponent();HeaderLogo.Source=CardLogo("header-hub",Asset("hub"),true)??HeaderLogo.Source;ConfigureMaximizedWorkArea();var assemblyVersion=typeof(MainWindow).Assembly.GetName().Version?.ToString(3)??"0.8.12";HubVersionText.Text="v"+assemblyVersion;Title="DJ RUSSO FX HUB — v"+assemblyVersion;try {var doc=JsonDocument.Parse(File.ReadAllText(PrefPath));lang=doc.RootElement.GetProperty("language").GetString()??"en"; if(doc.RootElement.TryGetProperty("theme",out var th))theme=th.GetString()??"neon";}catch{} var index=lang switch {"it"=>1,"es"=>2,"fr"=>3,"de"=>4,_=>0};SettingsLanguageBox.SelectedIndex=index;SettingsThemeBox.SelectedIndex=theme=="dark"?1:theme=="light"?2:0;LoadPlugins();LoadManifest();ready=true;ApplyTheme();Refresh();InitChipPlayer();Loaded+=async (_,_)=>await CheckHubReleaseAsync(false);}
 private void Header_MouseLeftButtonDown(object sender,System.Windows.Input.MouseButtonEventArgs e){
   if(e.ChangedButton!=System.Windows.Input.MouseButton.Left)return;
   // Never drag from a control: window buttons retain their own click handling.
   if(e.OriginalSource is System.Windows.DependencyObject origin){
     for(var element=origin;element!=null;element=System.Windows.Media.VisualTreeHelper.GetParent(element)){
       if(element is Button || element is System.Windows.Controls.Primitives.ButtonBase)return;
       if(ReferenceEquals(element,sender))break;
     }
   }
   if(e.ClickCount==2){ToggleWindowMaximized();e.Handled=true;return;}
   try{DragMove();}catch(System.InvalidOperationException){}
 }
 // Keep a borderless maximized window inside the current monitor's working area
 // (taskbar excluded), including on multi-monitor and mixed-DPI systems.
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
 private struct NativePoint { public int X,Y; }
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
 private struct NativeRect { public int Left,Top,Right,Bottom; }
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
 private struct NativeMinMaxInfo {
   public NativePoint Reserved,MaxSize,MaxPosition,MinTrackSize,MaxTrackSize;
 }
 [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential,CharSet=System.Runtime.InteropServices.CharSet.Auto)]
 private struct NativeMonitorInfo {
   public int Size;
   public NativeRect Monitor,Work;
   public uint Flags;
 }
 [System.Runtime.InteropServices.DllImport("user32.dll")]
 private static extern IntPtr MonitorFromWindow(IntPtr hwnd,uint flags);
 [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Auto)]
 [return:System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
 private static extern bool GetMonitorInfo(IntPtr monitor,ref NativeMonitorInfo info);
 private void ConfigureMaximizedWorkArea(){
   SourceInitialized+=(_,_)=>{
     if(System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(this).Handle) is { } source)
       source.AddHook(MaximizeWorkAreaHook);
   };
 }
 private IntPtr MaximizeWorkAreaHook(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled){
   const int WM_GETMINMAXINFO=0x0024;
   if(message!=WM_GETMINMAXINFO)return IntPtr.Zero;
   var monitor=MonitorFromWindow(hwnd,2);
   if(monitor==IntPtr.Zero)return IntPtr.Zero;
   var info=new NativeMonitorInfo{Size=System.Runtime.InteropServices.Marshal.SizeOf<NativeMonitorInfo>()};
   if(!GetMonitorInfo(monitor,ref info))return IntPtr.Zero;
   var limits=System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMinMaxInfo>(lParam);
   limits.MaxPosition.X=Math.Abs(info.Work.Left-info.Monitor.Left);
   limits.MaxPosition.Y=Math.Abs(info.Work.Top-info.Monitor.Top);
   limits.MaxSize.X=info.Work.Right-info.Work.Left;
   limits.MaxSize.Y=info.Work.Bottom-info.Work.Top;
   System.Runtime.InteropServices.Marshal.StructureToPtr(limits,lParam,false);
   handled=true;
   return IntPtr.Zero;
 }
 private void ToggleWindowMaximized(){
   WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;
   MaximizeWindowButton.Content=WindowState==WindowState.Maximized?"❐":"□";
 }
 private void MinimizeWindow_Click(object sender,RoutedEventArgs e)=>WindowState=WindowState.Minimized;
 private void MaximizeWindow_Click(object sender,RoutedEventArgs e)=>ToggleWindowMaximized();
 private void CloseWindow_Click(object sender,RoutedEventArgs e)=>Close();
 private void Social_Click(object sender,RoutedEventArgs e){if(sender is Button b && b.Tag is string url){try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message,"DJ RUSSO FX HUB");}}}
 private void LoadPlugins(){try {var path=Path.Combine(AppContext.BaseDirectory,"plugins.json");plugins=JsonSerializer.Deserialize<List<PluginInfo>>(File.ReadAllText(path),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??new();}catch(Exception ex){StatusText.Text=ex.Message;}}
 // Frozen, shared WPF bitmaps avoid repeated decoding and per-card allocations.
 private static readonly Dictionary<string,BitmapImage?> assetCache=new(StringComparer.OrdinalIgnoreCase);
 private static BitmapImage? Asset(string name){
   lock(assetCache){
     if(assetCache.TryGetValue(name,out var cached))return cached;
     try{
       var bitmap=new BitmapImage();
       bitmap.BeginInit();
       bitmap.UriSource=new Uri("pack://application:,,,/Assets/"+name+".png");
       bitmap.CacheOption=BitmapCacheOption.OnLoad;
       bitmap.DecodePixelWidth=720;
       bitmap.EndInit();
       bitmap.Freeze();
       return assetCache[name]=bitmap;
     }catch{return assetCache[name]=null;}
   }
 }
 // Normalize the *visible* bounds of the original transparent PNGs on coming-soon cards.
 // Preserves each artist logo and keeps the bundled source assets unchanged.
 private static readonly Dictionary<string,BitmapSource?> cardLogoCache=new(StringComparer.OrdinalIgnoreCase);
 private static BitmapSource? CardLogo(string id,BitmapImage? source,bool planned){
   if(source==null || !planned)return source;
   lock(cardLogoCache){
     if(cardLogoCache.TryGetValue(id,out var cached))return cached;
     BitmapSource chosen=source;
     try{
       BitmapSource argb=source.Format==PixelFormats.Bgra32
         ? source : new FormatConvertedBitmap(source,PixelFormats.Bgra32,null,0);
       int w=argb.PixelWidth,h=argb.PixelHeight;
       if(w>0 && h>0 && (long)w*h<5000000){
         var stride=checked(w*4);
         var pixels=new byte[checked(stride*h)];
         argb.CopyPixels(pixels,stride,0);
         int left=w,right=-1,top=h,bottom=-1;
         // Alpha threshold avoids antialiasing/glow specks from inflating the crop.
         for(int y=0;y<h;y++)
           for(int x=0;x<w;x++)
             if(pixels[y*stride+x*4+3]>=30){
               if(x<left)left=x;if(x>right)right=x;
               if(y<top)top=y;if(y>bottom)bottom=y;
             }
         if(right>=left && bottom>=top){
           int cw=right-left+1,ch=bottom-top+1;
           int pad=Math.Max(6,(int)(Math.Max(cw,ch)*0.045));
           int x0=Math.Max(0,left-pad),y0=Math.Max(0,top-pad);
           int x1=Math.Min(w,right+pad+1),y1=Math.Min(h,bottom+pad+1);
           // Ignore near-full-frame PNGs; do not accidentally cut full-bleed artwork.
           if(x1-x0<w*0.94 || y1-y0<h*0.94){
             var cropped=new CroppedBitmap(source,new Int32Rect(x0,y0,x1-x0,y1-y0));
             cropped.Freeze();
             chosen=cropped;
           }
         }
       }
     }catch{chosen=source;} // Malformed/opaque assets fall back to the unmodified source.
     return cardLogoCache[id]=chosen;
   }
 }
 // Debounce search so filesystem checks, SHA-256 validation and WPF cards are not rebuilt for each keystroke.
 private readonly System.Windows.Threading.DispatcherTimer searchDebounce=new(){Interval=TimeSpan.FromMilliseconds(250)};
 private void ApplySearchDebounce(object? sender,EventArgs e){searchDebounce.Stop();if(ready)Refresh();}

  private static void SetDropdownLabel(ComboBoxItem item, string value)
 {
     if (item.Content is StackPanel panel)
     {
         for (int i = panel.Children.Count - 1; i >= 0; i--)
             if (panel.Children[i] is TextBlock text) { text.Text = value; return; }
     }
     item.Content = value;
 }
private void Refresh()
 {
     SettingsButton.Content="⚙   "+T("settings"); GuideButton.Content="ⓘ   "+T("guide");
     ThemeHint.Text=T("theme");SettingsHint.Text=T("settingshint");
     SearchCaption.Text=T("searchProducts");FilterCaption.Text=T("filterProducts");
     ProductSearch.ToolTip=T("searchTip");ProductFilter.ToolTip=T("filterTip");
     // Keep filter captions synchronized with the selected UI language.
     string[][] filterLabels = {
         new[] { "All products", "Installed", "Available", "Coming soon" },
         new[] { "Tutti i prodotti", "Installati", "Disponibili", "In arrivo" },
         new[] { "Todos los productos", "Instalados", "Disponibles", "Próximamente" },
         new[] { "Tous les produits", "Installés", "Disponibles", "Prochainement" },
         new[] { "Alle Produkte", "Installiert", "Verfügbar", "Demnächst" }
     };
     for (int i = 0; i < 4; i++)
         if (ProductFilter.Items[i] is ComboBoxItem filterItem)
             SetDropdownLabel(filterItem, filterLabels[Index][i]);

     PreviewHeading.Text=new[]{"UPDATE WINDOW PREVIEW","ANTEPRIMA FINESTRA AGGIORNAMENTO","VISTA PREVIA DE ACTUALIZACIÓN","APERÇU DE LA MISE À JOUR","VORSCHAU UPDATE-FENSTER"}[Index];
     PreviewExplanation.Text=new[]{"Preview the Neon Ghost window without downloading or installing files.","Verifica la finestra Neon Ghost senza scaricare o installare file.","Prueba la ventana Neon Ghost sin descargar ni instalar archivos.","Teste la fenêtre Neon Ghost sans téléchargement ni installation.","Neon Ghost-Fenster testen, ohne Dateien herunterzuladen oder zu installieren."}[Index];
     PreviewUpdateDialogButton.Content=new[]{"PREVIEW DOWNLOAD","ANTEPRIMA DOWNLOAD","VISTA PREVIA","APERÇU DU TÉLÉCHARGEMENT","DOWNLOAD-VORSCHAU"}[Index];
     GitHubProjectButton.ToolTip=new[]{"Open the official GitHub repository","Apri il repository ufficiale GitHub","Abrir el repositorio oficial en GitHub","Ouvrir le dépôt GitHub officiel","Offizielles GitHub-Repository öffnen"}[Index];
     SecurityHeading.Text=new[]{"Security & authenticity","Sicurezza e autenticità","Seguridad y autenticidad","Sécurité et authenticité","Sicherheit und Authentizität"}[Index];
     SecurityExplanation.Text=new[]{"Official GitHub releases. SHA-256 checks file integrity, not malware safety. VirusTotal may have no report.","Release ufficiali GitHub. SHA-256 verifica l'integrità, non l'assenza di malware. VirusTotal potrebbe non avere un rapporto.","Versiones oficiales de GitHub. SHA-256 comprueba la integridad, no la ausencia de malware. Puede que VirusTotal no tenga informe.","Versions officielles GitHub. SHA-256 vérifie l'intégrité, pas l'absence de logiciels malveillants. Le rapport VirusTotal peut être absent.","Offizielle GitHub-Releases. SHA-256 prüft die Integrität, nicht die Malware-Freiheit. Ein VirusTotal-Bericht ist eventuell nicht verfügbar."}[Index];
     VirusTotalReportButton.Content=new[]{"Open VirusTotal upload","Apri caricamento VirusTotal","Abrir carga de VirusTotal","Ouvrir VirusTotal","VirusTotal-Upload öffnen"}[Index];
     CopyInstallerHashButton.Content=new[]{"Copy SHA-256","Copia SHA-256","Copiar SHA-256","Copier SHA-256","SHA-256 kopieren"}[Index];
     SourceText.Text=T("source");Subtitle.Text=T("subtitle");
     UpdatesButton.Content="↻   "+T("updates"); DiagnosticsButton.Content="◈   "+T("diagnostics");BackupButton.Content="▣   "+T("backup");FolderButton.Content="▣   "+T("folder");ScanButton.Content="⌕   "+T("scan");
     ShellSectionTitle.Text=BulkTitleLabels[Index];ShellSectionHint.Text=BulkHintLabels[Index];
     ShellInstallButton.Content=BulkInstallLabels[Index]; ShellRemoveButton.Content=BulkRemoveLabels[Index];
     StatusText.Text=T("ready");
     SetDropdownLabel((ComboBoxItem)SettingsThemeBox.Items[0],T("neon"));SetDropdownLabel((ComboBoxItem)SettingsThemeBox.Items[1],T("dark"));SetDropdownLabel((ComboBoxItem)SettingsThemeBox.Items[2],T("light"));
     var shellState=ShellInstaller.GetState();
     var shellStatusKey=shellState switch {
         ShellInstaller.State.Managed=>"shellManaged",
         ShellInstaller.State.ExternalSameBinary=>"shellExternal",
         ShellInstaller.State.Conflict=>"shellOther",
         ShellInstaller.State.ModifiedManaged=>"shellModified",
         _=>"shellMissing"};
     ShellSectionStatus.Text=T(shellStatusKey);
     // A conflicting VST3 must never be overwritten or removed automatically.
     // Explain disabled actions in the selected language.
     ShellInstallButton.ToolTip=BulkHintLabels[Index];
     ShellRemoveButton.ToolTip=BulkHintLabels[Index];
     ShellInstallButton.IsEnabled=(shellState==ShellInstaller.State.Missing && ShellInstaller.HasExpectedPayload()) || (!File.Exists(MeterExePath) && File.Exists(BundledMeterPath));
     ShellRemoveButton.IsEnabled=shellState==ShellInstaller.State.Managed || File.Exists(MeterExePath);
     // Selected-product controls are updated after the card list is rebuilt.
     bool shellVerified=shellState is ShellInstaller.State.Managed or ShellInstaller.State.ExternalSameBinary;
     var verifiedIds=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
     if(shellVerified)
       foreach(var pkg in manifest.packages)
         if(pkg.id=="fx-shell" && pkg.detector=="bundled-shell-sha256")
           foreach(var id in pkg.effectIds)verifiedIds.Add(id);
     var cards=new List<PluginCard>();
     foreach(var fx in plugins)
     {
         bool planned=fx.status=="Planned" && fx.id!="meter-x";
         var logo=fx.id switch {"sidechain-x"=>Asset("sidechain"),"reverse-fx"=>Asset(theme=="light"?"reverse_light":"reverse"),"stutter-fx"=>Asset("stutter"),"meter-x"=>Asset("meter-x"),_=>Asset(fx.id)};
         var category=fx.category switch {"Dynamics"=>"dynamics","Creative FX"=>"creative","Analysis"=>"analysis","Mastering"=>"mastering","Vocal"=>"vocal","EQ"=>"eq","Spatial"=>"spatial","Saturation"=>"saturation","AI Offline"=>"ai",_=>"creative"};
         bool installed=fx.id=="meter-x" ? File.Exists(MeterExePath) : (!planned && verifiedIds.Contains(fx.id));
         cards.Add(new PluginCard {
             Id=fx.id,IsSelected=fx.id==selectedProductId,IsReady=false,
             BadgeSymbol=installed?"✓":planned?"…":"✕",
             BadgeBrush=installed?System.Windows.Media.Brushes.LimeGreen:planned?System.Windows.Media.Brushes.MediumPurple:System.Windows.Media.Brushes.IndianRed,
             BadgeTip=installed?(Index==1?"Installato e verificato":"Installed and verified"):
                 (planned?(Index==1?"Non ancora rilasciato":"Not released yet"):(Index==1?"Non installato":"Not installed")),
             FallbackName="",MeterVisibility=fx.id=="meter-x"?Visibility.Visible:Visibility.Collapsed,MeterReady=fx.id=="meter-x" && File.Exists(MeterExePath),MeterInstallReady=fx.id=="meter-x" && !File.Exists(MeterExePath) && File.Exists(BundledMeterPath),MeterImportLabel=MeterInstallLabels[Index],MeterLaunchLabel=MeterLaunchLabels[Index],MeterRemoveLabel=MeterRemoveLabels[Index],Logo=CardLogo(fx.id,logo,planned),LogoHeight=planned?95:77,Category=T(category),
             Footer=fx.id=="meter-x"? ("v1.2.6 • " + (installed?MeterText("installed"):MeterText("missing"))) : planned?T("planned"):(installed?(Index==1?"INSTALLATO":"INSTALLED"):(Index==1?"NON INSTALLATO":"NOT INSTALLED"))+"\n"+ShellInstaller.VersionLabel
         });
     }
     var term=ProductSearch?.Text?.Trim()??"";
     var filter=ProductFilter?.SelectedIndex??0;
     PluginList.ItemsSource=cards.Where(card=>(term.Length==0 || plugins.Any(p=>p.id==card.Id && (p.name.Contains(term,StringComparison.OrdinalIgnoreCase)||p.category.Contains(term,StringComparison.OrdinalIgnoreCase)))) && (filter==0 || (filter==1 && (card.BadgeSymbol=="✓")) || (filter==2 && card.BadgeSymbol=="✕") || (filter==3 && card.BadgeSymbol=="…"))).ToList();
     if(DashboardText!=null)DashboardText.Text=$"{T("products")}: {cards.Count}    •    {T("installedShort")}: {cards.Count(q=>q.BadgeSymbol=="✓")}    •    {T("comingSoonShort")}: {cards.Count(q=>q.BadgeSymbol=="…")}";
     UpdateSelectedProductPanel();
     UpdateMeterUi();
 }
 private void SettingsLanguageBox_SelectionChanged(object sender,SelectionChangedEventArgs e){if(!ready||SettingsLanguageBox.SelectedItem is not ComboBoxItem item)return;lang=item.Tag?.ToString()??"en";ScanResultsPanel.Visibility=Visibility.Collapsed;try{Directory.CreateDirectory(Path.GetDirectoryName(PrefPath)!);File.WriteAllText(PrefPath,JsonSerializer.Serialize(new{language=lang,theme}));}catch{}Refresh();}

 private void SettingsThemeBox_SelectionChanged(object sender,SelectionChangedEventArgs e){
  if(!ready)return;theme=SettingsThemeBox.SelectedIndex switch {1=>"dark",2=>"light",_=>"neon"};
  SavePrefs();ApplyTheme();Refresh();
 }
 private void SavePrefs(){try{Directory.CreateDirectory(Path.GetDirectoryName(PrefPath)!);File.WriteAllText(PrefPath,JsonSerializer.Serialize(new{language=lang,theme}));}catch{}}
 private void ApplyTheme(){
  var light=theme=="light"; var dark=theme=="dark";
  var bg=light?"#EDF1F8":dark?"#11141B":"#090C19";
  var panel=light?"#FFFFFF":dark?"#222936":"#151A30";
  var fg=light?"#14243A":"#F2F5FF";
  var accent=light?"#3476D7":dark?"#7397CE":"#A36AFF";
  var secondary=light?"#174B80":dark?"#9CE6EF":"#7DEFFF";
  var logoPlate=light?"#FFFFFF":dark?"#111B30":"#0D142C";
  var info=light?"#253957":dark?"#E0E9F8":"#CAD5F1";
  foreach(var (key,value) in new[]{("BgBrush",bg),("PanelBrush",panel),("FgBrush",fg),("AccentBrush",accent),("SecondaryBrush",secondary),("LogoPlateBrush",logoPlate),("InfoBrush",info)})
   Resources[key]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
  var neonCyan=light?"#007D9A":dark?"#6CAFC8":"#00EFFF";
  var neonPink=light?"#8A4FA6":dark?"#9B6CC4":"#F33AFF";
  var neonGreen=light?"#18834C":dark?"#52BA88":"#24EF85";
  var cardStroke=light?"#9CB1D6":dark?"#55627F":"#7864DD";
  var sideBg=light?"#F0F4FC":dark?"#191E2B":"#101127";
  Resources["NavSelectedBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#DDEBFF" : dark ? "#263B55" : "#213352"));
  foreach(var (key,value) in new[]{("NeonCyanBrush",neonCyan),("NeonPinkBrush",neonPink),("NeonGreenBrush",neonGreen),("CardStrokeBrush",cardStroke),("SidebarBrush",sideBg)})
   Resources[key]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
  var cardStops=light?new[]{"#FFFFFF","#F1F6FE","#EBF1FD"}:dark?new[]{"#242A39","#1B2231","#242036"}:new[]{"#14152D","#111329","#1A0C2D"};
  Resources["CardFillBrush"]=new LinearGradientBrush(new GradientStopCollection{
   new GradientStop((Color)ColorConverter.ConvertFromString(cardStops[0]),0),
   new GradientStop((Color)ColorConverter.ConvertFromString(cardStops[1]),0.6),
   new GradientStop((Color)ColorConverter.ConvertFromString(cardStops[2]),1)},new Point(0,0),new Point(1,1));
  var headerStops=light?new[]{"#EDF3FC","#E8EAFB","#EDF3FC"}:dark?new[]{"#161A27","#222239","#161A27"}:new[]{"#080B1E","#16112B","#080B1E"};
  Resources["HeaderFillBrush"]=new LinearGradientBrush(new GradientStopCollection{
   new GradientStop((Color)ColorConverter.ConvertFromString(headerStops[0]),0),
   new GradientStop((Color)ColorConverter.ConvertFromString(headerStops[1]),0.55),
   new GradientStop((Color)ColorConverter.ConvertFromString(headerStops[2]),1)},new Point(0,0),new Point(1,0));
  // Native WPF selected-item surface stays light even in dark themes.
  // Use plain text content + dark selection text for reliable visibility at all DPI settings.
  foreach(var (key,value) in new[]{
   ("ComboSurfaceBrush","#FFFFFF"),
   ("ComboTextBrush","#15213A"),
   ("ComboHoverBrush","#C9DDFB"),
   ("ComboSelectedBrush","#DCE5FB"),
   ("ComboBorderBrush",light?"#7854CE":dark?"#7485B9":"#805EE4"),
   ("ComboFocusBrush",light?"#4275CE":"#61E4FF")
  })Resources[key]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
  var installBg=light?"#E2F8EA":dark?"#18372C":"#0D3729";
  var removeBg=light?"#F9E4F7":dark?"#392B40":"#392044";
  var launchBg=light?"#E3F7FB":dark?"#1B3841":"#0C3846";
  foreach(var b in new[]{ShellInstallButton,MeterPanelInstallButton}){b.Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(installBg));b.BorderBrush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(light?"#16804D":"#24E977"));b.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(light?"#173B2A":"#FFFFFF"));}
  foreach(var b in new[]{ShellRemoveButton,MeterPanelRemoveButton}){b.Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(removeBg));b.BorderBrush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(light?"#9D3A96":"#E13AF5"));b.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(light?"#50254D":"#FFFFFF"));}
  MeterPanelLaunchButton.Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(launchBg));
  MeterPanelLaunchButton.BorderBrush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(light?"#17788B":"#3FEFFF"));
  MeterPanelLaunchButton.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(light?"#164D59":"#FFFFFF"));
  Resources["HeadingBrush"] = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(light?"#065C91":"#71F4FF"),(Color)ColorConverter.ConvertFromString(light?"#7A3CC9":"#D58BFF"),0);
 }

 private static readonly string[] BulkInstallLabels={"INSTALL ALL","INSTALLA TUTTO","INSTALAR TODO","TOUT INSTALLER","ALLES INSTALLIEREN"};
 private static readonly string[] BulkRemoveLabels={"REMOVE ALL","RIMUOVI TUTTO","ELIMINAR TODO","TOUT SUPPRIMER","ALLES ENTFERNEN"};
 private static readonly string[] BulkTitleLabels={"AVAILABLE SOFTWARE","SOFTWARE DISPONIBILI","SOFTWARE DISPONIBLE","LOGICIELS DISPONIBLES","VERFÜGBARE SOFTWARE"};
 private static readonly string[] BulkHintLabels={"Install bundled FX SHELL VST3 and METER X EXE.","Installa FX SHELL VST3 e METER X EXE inclusi.","Instala FX SHELL y METER X incluidos.","Installez FX SHELL et METER X inclus.","FX SHELL und METER X installieren."};
 private static readonly string[] MeterInstallLabels={"INSTALL","INSTALLA","INSTALAR","INSTALLER","INSTALLIEREN"};
 private string BundledMeterPath=>Path.Combine(AppContext.BaseDirectory,"Payload","DJ RUSSO METER X.exe");
 private bool InstallMeterFile(out string detail){
  detail="";
  if(File.Exists(MeterExePath)){detail="METER X: already installed / già installato";return true;}
  if(!File.Exists(BundledMeterPath)){detail="METER X: EXE missing / EXE incluso mancante";return false;}
  try{
   Directory.CreateDirectory(Path.GetDirectoryName(MeterExePath)!);
   File.Copy(BundledMeterPath,MeterExePath,false);
   detail="METER X: installed / installato";return true;
  }catch(Exception ex){detail="METER X: "+ex.Message;return false;}
 }
 private void InstallShell_Click(object sender,RoutedEventArgs e){
  if(MessageBox.Show(this,Index==1?
     "Installare i componenti disponibili? FX SHELL richiede autorizzazione amministratore. Chiudi prima FL Studio.":
     "Install available components? FX SHELL requires administrator approval. Close FL Studio first.",
     "DJ RUSSO FX HUB",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  var messages=new List<string>();
  var state=ShellInstaller.GetState();
  if(state==ShellInstaller.State.Missing){
   if(!ShellInstaller.HasExpectedPayload())messages.Add("FX SHELL: "+T("payloadBad"));
   else{
    var result=RunElevated(ShellInstaller.InstallArgument);
    if(result==0){
     try{ShellInstaller.SaveReceipt();messages.Add("FX SHELL: OK");}
     catch(Exception ex){messages.Add("FX SHELL: receipt / registrazione: "+ex.Message);}
    }else messages.Add("FX SHELL: "+T(result==9?"cancelled":"ioError")+" ("+result+")");
   }
  }else if(state==ShellInstaller.State.Managed || state==ShellInstaller.State.ExternalSameBinary)
    messages.Add("FX SHELL: already present / già presente");
  else messages.Add("FX SHELL: "+T("shellConflict"));
  InstallMeterFile(out var meterDetail);messages.Add(meterDetail);
  Refresh();
  MessageBox.Show(this,string.Join("\n",messages),"DJ RUSSO FX HUB",MessageBoxButton.OK,MessageBoxImage.Information);
 }
 private void RemoveShell_Click(object sender,RoutedEventArgs e){
  if(MessageBox.Show(this,Index==1?
     "Rimuovere FX SHELL e METER X gestiti dall'HUB? I file esterni non saranno toccati. Chiudi FL Studio e METER X.":
     "Remove HUB-managed FX SHELL and METER X? External files are untouched. Close FL Studio and METER X.",
     "DJ RUSSO FX HUB",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
  var messages=new List<string>();
  if(ShellInstaller.GetState()==ShellInstaller.State.Managed){
   var result=RunElevated(ShellInstaller.RemoveArgument);
   if(result==0){
    try{ShellInstaller.DeleteReceipt();messages.Add("FX SHELL: removed / rimossa");}
    catch(Exception ex){messages.Add("FX SHELL: receipt / registrazione: "+ex.Message);}
   }else messages.Add("FX SHELL: "+T(result==9?"cancelled":"ioError")+" ("+result+")");
  }else messages.Add("FX SHELL: no HUB-managed file / nessun file gestito dall'HUB");
  if(File.Exists(MeterExePath)){
   try{File.Delete(MeterExePath);messages.Add("METER X: removed / rimosso");}
   catch(Exception ex){messages.Add("METER X: "+ex.Message);}
  }else messages.Add("METER X: not installed / non installato");
  Refresh();
  MessageBox.Show(this,string.Join("\n",messages),"DJ RUSSO FX HUB",MessageBoxButton.OK,MessageBoxImage.Information);
 }
 private int RunElevated(string operation)
 {
     try
     {
         var exe=Environment.ProcessPath;
         if(string.IsNullOrWhiteSpace(exe)) return 8;
         var psi=new ProcessStartInfo(exe){Arguments=operation,UseShellExecute=true,Verb="runas",WorkingDirectory=AppContext.BaseDirectory};
         using var worker=Process.Start(psi);
         if(worker==null)return 8;
         worker.WaitForExit();
         return worker.ExitCode;
     }
     catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 1223) { return 9; }
     catch { return 8; }
 }
 private void ShowInstallResult(int code)
 {
     var key=code switch {2=>"payloadBad",3=>"shellConflict",4=>"shellChanged",5=>"permissionError",6=>"ioError",7=>"notManaged",9=>"cancelled",_=>"unknownError"};
     MessageBox.Show(T(key),"DJ RUSSO FX HUB",MessageBoxButton.OK,MessageBoxImage.Warning);
 }
 private void ProductSearch_Changed(object sender,System.Windows.Controls.TextChangedEventArgs e){if(ready){searchDebounce.Stop();searchDebounce.Start();}}
 private void ProductFilter_Changed(object sender,SelectionChangedEventArgs e){if(ready){searchDebounce.Stop();Refresh();}}
 private string BackupDir=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DJRusso","FXHub","Backups");
 private void Backup_Click(object sender,RoutedEventArgs e){
  try{var dir=Path.Combine(BackupDir,DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));Directory.CreateDirectory(dir);
   foreach(var source in new[]{PrefPath,ManagedPath,MeterExePath})if(File.Exists(source)){var name=source==MeterExePath?"METER_X.exe":Path.GetFileName(source);File.Copy(source,Path.Combine(dir,name),false);}
   File.WriteAllText(Path.Combine(dir,"README.txt"),"DJ RUSSO FX HUB local snapshot. Do not copy files to protected VST3 locations manually. FX SHELL binary is not included; installer receipt is informational only.");
   MessageBox.Show(this,(Index==1?"Backup creato:\n":"Backup created:\n")+dir,"FX HUB");
  }catch(Exception ex){MessageBox.Show(this,ex.Message,"Backup",MessageBoxButton.OK,MessageBoxImage.Warning);}
 }
 private void Diagnostics_Click(object sender,RoutedEventArgs e){
  // Display only localized text; the underlying tests remain read-only and unchanged.
  var vst=VstDir;
  var flPaths=new[]{"2024","2025","2026"}.Select(y=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"Image-Line","FL Studio "+y));
  var shellState=ShellInstaller.GetState();
  var localizedShellState=shellState switch{
   ShellInstaller.State.Missing=>T("diagShellMissing"),
   ShellInstaller.State.Managed=>T("diagShellManaged"),
   ShellInstaller.State.ExternalSameBinary=>T("diagShellExternal"),
   ShellInstaller.State.Conflict=>T("diagShellConflict"),
   ShellInstaller.State.ModifiedManaged=>T("diagShellModified"),
   _=>T("diagShellUnknown")
  };
  var vstStatus=Directory.Exists(vst)?T("diagVstFound"):T("diagVstMissing");
  var meterStatus=File.Exists(MeterExePath)?T("diagMeterFound"):T("diagMeterMissing");
  var packageStatus=ShellInstaller.HasExpectedPayload()?T("diagPackageValid"):T("diagPackageInvalid");
  var flStatus=flPaths.Any(Directory.Exists)?T("diagFlFound"):T("diagFlMissing");
  // Read-only detection of known standalone file names. Presence does not prove VST3 identity.
  var knownVst3Names=new[]{"DJ RUSSO SIDECHAIN X.vst3","DJ RUSSO REVERSE FX.vst3","DJ RUSSO STUTTER FX.vst3","DJ RUSSO VOCAL FX.vst3"};
  var foundStandalone=knownVst3Names.Where(name=>{
    var path=Path.Combine(vst,name);
    return File.Exists(path)||Directory.Exists(path);
  }).ToArray();
  var detectionHeading=new[]{"Standalone VST3 candidates (filename only)","Possibili VST3 indipendenti (solo nome file)","Posibles VST3 independientes (solo nombre)","VST3 indépendants possibles (nom uniquement)","Mögliche eigenständige VST3 (nur Dateiname)"}[Index];
  var detectionList=foundStandalone.Length==0?"—":string.Join(", ",foundStandalone);
  var report=$"{T("diagVstFolder")}: {vstStatus}\n{vst}\n\nFX SHELL: {localizedShellState}\nMETER X: {meterStatus}\n{T("diagPackage")}: {packageStatus}\n\n{T("diagFlLocations")}: {flStatus}\n\n{detectionHeading}: {detectionList}\n\n{T("diagReadOnly")}";
  MessageBox.Show(this,report,"DJ RUSSO • "+T("diagTitle"),MessageBoxButton.OK,MessageBoxImage.Information);
 }
 private static readonly System.Net.Http.HttpClient UpdateClient = CreateUpdateClient();
 private static System.Net.Http.HttpClient CreateUpdateClient(){
   var c=new System.Net.Http.HttpClient{Timeout=TimeSpan.FromMinutes(5)};
   c.DefaultRequestHeaders.UserAgent.ParseAdd("DJRussoFXHub/0.8.29");
   c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
   return c;
 }
 private bool hubUpdateInProgress;
 private async void CheckUpdates_Click(object sender,RoutedEventArgs e){await CheckHubReleaseAsync(true);}
 private async System.Threading.Tasks.Task CheckHubReleaseAsync(bool interactive){
   const string releasesUrl="https://github.com/djrusso1994/DJ-RUSSO-FX-HUB/releases";
   if(hubUpdateInProgress)return;
   try{
     using var metadataTimeout=new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15));
     using var response=await UpdateClient.GetAsync("https://api.github.com/repos/djrusso1994/DJ-RUSSO-FX-HUB/releases?per_page=30",metadataTimeout.Token);
     response.EnsureSuccessStatusCode();
     using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
     JsonElement root=default;
     Version? newest=null;
     foreach(var release in doc.RootElement.EnumerateArray()){
       if(release.TryGetProperty("draft",out var draft) && draft.GetBoolean())continue;
       var tagName=release.GetProperty("tag_name").GetString()??"";
       if(!Version.TryParse(tagName.TrimStart('v','V'),out var parsed))continue;
       if(newest==null || parsed>newest){newest=parsed;root=release;}
     }
     if(newest==null){if(interactive)MessageBox.Show(this,Index==1?"Nessuna versione pubblicata su GitHub.":"No GitHub releases are available.","DJ RUSSO FX HUB");return;}
     var tag=root.GetProperty("tag_name").GetString()??"";
     var page=root.GetProperty("html_url").GetString()??releasesUrl;
     if(!page.StartsWith(releasesUrl,StringComparison.OrdinalIgnoreCase))page=releasesUrl;
     var current=typeof(MainWindow).Assembly.GetName().Version??new Version(0,8,15);
     if(!Version.TryParse(tag.TrimStart('v','V'),out var available))return;
     if(available<=current){
       if(interactive){
         string[] upToDateMessages={
           "DJ RUSSO FX HUB is up to date. Installed version: v"+current.ToString(3)+".",
           "DJ RUSSO FX HUB è aggiornato. Versione installata: v"+current.ToString(3)+".",
           "DJ RUSSO FX HUB está actualizado. Versión instalada: v"+current.ToString(3)+".",
           "DJ RUSSO FX HUB est à jour. Version installée : v"+current.ToString(3)+".",
           "DJ RUSSO FX HUB ist aktuell. Installierte Version: v"+current.ToString(3)+"."
         };
         MessageBox.Show(this,upToDateMessages[Index],"DJ RUSSO FX HUB",MessageBoxButton.OK,MessageBoxImage.Information);
       }
       return;
     }
     var expectedName="DJ_RUSSO_FX_HUB_Setup_v"+available.ToString(3)+".exe";
     string? assetUrl=null,sha256=null;
     if(root.TryGetProperty("assets",out var assets)){
       foreach(var asset in assets.EnumerateArray()){
         if(!string.Equals(asset.GetProperty("name").GetString(),expectedName,StringComparison.OrdinalIgnoreCase))continue;
         assetUrl=asset.GetProperty("browser_download_url").GetString();
         if(asset.TryGetProperty("digest",out var digest) && digest.ValueKind==JsonValueKind.String){
           var value=digest.GetString()??"";
           if(value.StartsWith("sha256:",StringComparison.OrdinalIgnoreCase))sha256=value.Substring(7);
         }
         break;
       }
     }
     // No unverified executable is ever launched. Older releases without a SHA-256 digest
     // remain available through the normal GitHub download page.
     if(assetUrl==null || sha256==null || sha256.Length!=64 ||
        !sha256.All(c=>Uri.IsHexDigit(c)) ||
        !Uri.TryCreate(assetUrl,UriKind.Absolute,out var assetUri) ||
        assetUri.Scheme!="https" || assetUri.Host!="github.com" ||
        !assetUri.AbsolutePath.StartsWith("/djrusso1994/DJ-RUSSO-FX-HUB/releases/download/",StringComparison.OrdinalIgnoreCase)){
       var prompt=Index==1?$"È disponibile la v{available}. Per sicurezza questa Release richiede il download manuale da GitHub. Aprire la pagina?":$"Version {available} is available. This release requires manual download for security. Open GitHub?";
       if(MessageBox.Show(this,prompt,"DJ RUSSO FX HUB",MessageBoxButton.YesNo,MessageBoxImage.Information)==MessageBoxResult.Yes)
         Process.Start(new ProcessStartInfo(page){UseShellExecute=true});
       return;
     }
     var confirm=Index==1?$"DJ RUSSO FX HUB v{available} disponibile.\nScaricare l'installer ufficiale, verificare SHA-256 e avviare l'aggiornamento?\nWindows potrebbe chiedere autorizzazione amministratore.":$"DJ RUSSO FX HUB v{available} available.\nDownload and verify the official installer, then launch the update?\nWindows may request administrator permission.";
     if(MessageBox.Show(this,confirm,"DJ RUSSO FX HUB",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
     hubUpdateInProgress=true;
     UpdatesButton.IsEnabled=false;
     var updateDir=Path.Combine(Path.GetTempPath(),"DJRussoFXHub","Updates");
     Directory.CreateDirectory(updateDir);
     var installer=Path.Combine(updateDir,expectedName);
     var partial=installer+".download";
     using var downloadCancel=new System.Threading.CancellationTokenSource();
     var (progressWindow,progressStatus,progressBar,progressDetails,cancelButton)=CreateUpdateProgressDialog(
       ()=>downloadCancel.Cancel(),()=>downloadCancel.IsCancellationRequested);
     progressWindow.Show();
     try{
       StatusText.Text=Index==1?"Download aggiornamento in corso...":"Downloading update...";
       using(var download=await UpdateClient.GetAsync(assetUri,System.Net.Http.HttpCompletionOption.ResponseHeadersRead,downloadCancel.Token)){
         download.EnsureSuccessStatusCode();
         var total=download.Content.Headers.ContentLength;
         progressBar.IsIndeterminate=!total.HasValue || total.Value<=0;
         await using var source=await download.Content.ReadAsStreamAsync();
         await using var target=new FileStream(partial,FileMode.Create,FileAccess.Write,FileShare.None,81920,true);
         var buffer=new byte[81920];
         long received=0;
         var timer=System.Diagnostics.Stopwatch.StartNew();
         long lastUiUpdateMs=-250;
         int count;
         while((count=await source.ReadAsync(buffer.AsMemory(0,buffer.Length),downloadCancel.Token))>0){
           await target.WriteAsync(buffer.AsMemory(0,count),downloadCancel.Token);
           received+=count;
           var mb=received/1048576.0;
           var speed=timer.Elapsed.TotalSeconds>0?mb/timer.Elapsed.TotalSeconds:0;
           // Limit WPF updates to ~4 per second instead of refreshing on every 80 KB chunk.
           if(timer.ElapsedMilliseconds-lastUiUpdateMs<250 && (!total.HasValue || received<total.Value))continue;
           lastUiUpdateMs=timer.ElapsedMilliseconds;
           if(total.HasValue && total.Value>0){
             var percent=Math.Min(100,received*100.0/total.Value);
             progressBar.Value=percent;
             progressStatus.Text=(Index==1?"Download installer":"Downloading installer")+" • "+percent.ToString("F0")+"%";
             progressDetails.Text=$"{mb:F1} / {total.Value/1048576.0:F1} MB   •   {speed:F1} MB/s";
           }else{
             progressStatus.Text=Index==1?"Download installer...":"Downloading installer...";
             progressDetails.Text=$"{mb:F1} MB   •   {speed:F1} MB/s";
           }
         }
       }
       cancelButton.IsEnabled=false;
       progressBar.IsIndeterminate=true;
       progressStatus.Text=Index==1?"Verifica sicurezza SHA-256...":"Verifying SHA-256...";
       progressDetails.Text=Index==1?"Download completato. Verifica dell'installer.":"Download complete. Verifying installer.";
       await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
       string actual;
       using(var input=File.OpenRead(partial)){
         var hash=await System.Security.Cryptography.SHA256.HashDataAsync(input);
         actual=Convert.ToHexString(hash);
       }
       if(!string.Equals(actual,sha256,StringComparison.OrdinalIgnoreCase))
         throw new InvalidDataException("Installer SHA-256 verification failed.");
       File.Move(partial,installer,true);
       progressStatus.Text=Index==1?"Avvio installazione...":"Launching installer...";
       await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
       Process.Start(new ProcessStartInfo(installer){UseShellExecute=true,WorkingDirectory=updateDir});
       Application.Current.Shutdown();
     }catch(OperationCanceledException){
       try{if(File.Exists(partial))File.Delete(partial);}catch{}
       StatusText.Text=Index==1?"Download annullato":"Download cancelled";
     }catch(Exception ex){
       try{if(File.Exists(partial))File.Delete(partial);}catch{}
       MessageBox.Show(this,(Index==1?"Aggiornamento non completato: ":"Update not completed: ")+ex.Message,"DJ RUSSO FX HUB",MessageBoxButton.OK,MessageBoxImage.Warning);
     }finally{
       // Allow programmatic closure after cancellation, error, or successful installation.
       cancelButton.IsEnabled=false;
       progressWindow.Close();
       hubUpdateInProgress=false;
       UpdatesButton.IsEnabled=true;
       StatusText.Text=T("ready");
     }
   }catch(Exception ex){if(interactive)MessageBox.Show(this,(Index==1?"Impossibile controllare GitHub: ":"Could not check GitHub: ")+ex.Message,"DJ RUSSO FX HUB",MessageBoxButton.OK,MessageBoxImage.Warning);}
 }
 private void OpenFolder_Click(object sender,RoutedEventArgs e){var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),"VST3");if(Directory.Exists(path))Process.Start(new ProcessStartInfo{FileName=path,UseShellExecute=true});else MessageBox.Show(T("missingfolder")+path);}

 private static readonly string[] MeterImportLabels={"IMPORT EXE","IMPORTA EXE","IMPORTAR EXE","IMPORTER EXE","EXE IMPORTIEREN"};
 private static readonly string[] MeterLaunchLabels={"LAUNCH","AVVIA","ABRIR","LANCER","STARTEN"};
 private static readonly string[] MeterRemoveLabels={"REMOVE","RIMUOVI","ELIMINAR","SUPPRIMER","ENTFERNEN"};
 private static readonly string[] MeterMissingLabels={"METER X ready to install","METER X pronto da installare","METER X no importado","METER X non importé","METER X nicht importiert"};
 private static readonly string[] MeterInstalledLabels={"Portable EXE imported — ready to launch","EXE portable importato — pronto per l'avvio","EXE portable importado — listo","EXE portable importé — prêt","Portable EXE importiert — bereit"};
 private string MeterText(string state)=>state=="installed"?MeterInstalledLabels[Index]:MeterMissingLabels[Index];
 private string MeterExePath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DJRusso","FXHub","MeterX","DJ RUSSO METER X.exe");

 private bool IsShellProduct(string id)=>id=="sidechain-x" || id=="reverse-fx" || id=="stutter-fx";
 private void ProductCard_Click(object sender,System.Windows.Input.MouseButtonEventArgs e){
  if(sender is FrameworkElement el && el.DataContext is PluginCard card){
   selectedProductId=card.Id;
   Refresh();
  }
 }
 private void UpdateSelectedProductPanel(){
  var fx=plugins.FirstOrDefault(p=>p.id==selectedProductId);
  if(fx==null){selectedProductId="meter-x";fx=plugins.FirstOrDefault(p=>p.id=="meter-x");}
  if(fx==null)return;
  bool meter=fx.id=="meter-x",shell=IsShellProduct(fx.id);
  var shellState=ShellInstaller.GetState();
  bool installed=meter?File.Exists(MeterExePath):shell && shellState is ShellInstaller.State.Managed or ShellInstaller.State.ExternalSameBinary;
  ProductPanelTitle.Text=fx.name;
  ProductPanelLogo.Source=Asset(fx.id switch{"sidechain-x"=>"sidechain","reverse-fx"=>theme=="light"?"reverse_light":"reverse","stutter-fx"=>"stutter",_=>fx.id});
  ProductPanelVersion.Text=meter?"v1.2.6 • NEON GHOST":shell?ShellInstaller.VersionLabel:"COMING SOON / IN ARRIVO";
  ProductPanelDescription.Text=meter?
    "Spectrum · Waveform · Spectrogram · Loudness · Peak/RMS · Vectorscope · Frequency Bands · Correlation":
    shell?(Index==1?"Questo effetto è incluso nel pacchetto FX SHELL VST3. L'installazione riguarda tutti e tre gli effetti.":"This effect is part of the FX SHELL VST3 package. Installing it installs all three effects."):
    (Index==1?"Plugin in sviluppo. Nessun file installabile incluso.":"Plugin in development. No installable package bundled.");
  MeterPanelInstallButton.Content=meter?(Index==1?"INSTALLA METER X":"INSTALL METER X"):shell?(Index==1?"INSTALLA FX SHELL":"INSTALL FX SHELL"):(Index==1?"IN ARRIVO":"COMING SOON");
  MeterPanelRemoveButton.Content=meter?(Index==1?"RIMUOVI METER X":"REMOVE METER X"):(Index==1?"RIMUOVI FX SHELL":"REMOVE FX SHELL");
  MeterPanelLaunchButton.Content=Index==1?"▶  AVVIA":"▶  LAUNCH";
  MeterPanelInstallButton.IsEnabled=meter?(!installed && File.Exists(BundledMeterPath)):shell?(shellState==ShellInstaller.State.Missing && ShellInstaller.HasExpectedPayload()):false;
  MeterPanelRemoveButton.IsEnabled=meter?installed:shell && shellState==ShellInstaller.State.Managed;
  MeterPanelLaunchButton.Visibility=meter?Visibility.Visible:Visibility.Collapsed;
  MeterPanelLaunchButton.IsEnabled=meter && installed;
  MeterPanelRemoveButton.Visibility=meter||shell?Visibility.Visible:Visibility.Collapsed;
  MeterPanelState.Text=installed?(Index==1?"● Installato":"● Installed"):meter||shell?(Index==1?"○ Non installato":"○ Not installed"):(Index==1?"◌ In arrivo":"◌ Coming soon");
 }
 private async void CheckPluginUpdates_Click(object sender,RoutedEventArgs e){
   if(hubUpdateInProgress)return;
   PluginUpdatesButton.IsEnabled=false;
   try{
     var id=selectedProductId;
     var packageId=IsShellProduct(id)?"fx-shell":id;
     var installed=packageId=="fx-shell"
       ? ShellInstaller.GetState() is ShellInstaller.State.Managed or ShellInstaller.State.ExternalSameBinary
       : packageId=="meter-x" && File.Exists(MeterExePath);
     if(!installed){
       MessageBox.Show(this,Index==1?"Questo plugin non risulta installato.":"This plugin is not installed.","DJ RUSSO FX HUB",MessageBoxButton.OK,MessageBoxImage.Information);
       return;
     }
     using var response=await UpdateClient.GetAsync("https://raw.githubusercontent.com/djrusso1994/DJ-RUSSO-FX-HUB/main/plugin-updates.json");
     response.EnsureSuccessStatusCode();
     using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
     if(document.RootElement.GetProperty("schemaVersion").GetInt32()!=1)throw new InvalidDataException("Unsupported plugin catalog schema.");
     var entries=document.RootElement.GetProperty("packages").EnumerateArray();
     JsonElement found=default;
     foreach(var entry in entries){
       if(entry.GetProperty("id").GetString()==packageId){found=entry;break;}
     }
     if(found.ValueKind==JsonValueKind.Undefined)throw new InvalidDataException("Plugin not found in official catalog.");
     var version=found.GetProperty("version").GetString()??"";
     var available=found.GetProperty("downloadAvailable").GetBoolean();
     var installedVersion=packageId=="fx-shell"?ShellInstaller.VersionLabel:"1.2.6";
     var message=Index==1
       ? $"Pacchetto: {packageId}\\nVersione inclusa di riferimento: {installedVersion}\\nUltima versione nel catalogo: {version}\\n\\n"+(available?"Pacchetto pubblicato: l'installazione automatica richiede ancora verifica di integrità e compatibilità.":"Nessun aggiornamento autonomo del plugin disponibile. I file installati non saranno modificati.")
       : $"Package: {packageId}\\nBundled reference version: {installedVersion}\\nCatalog version: {version}\\n\\n"+(available?"Published package: automatic installation requires integrity and compatibility validation.":"No standalone plugin update available. Installed files will not be modified.");
     MessageBox.Show(this,message,"DJ RUSSO FX HUB • PLUGIN UPDATES",MessageBoxButton.OK,MessageBoxImage.Information);
   }catch(Exception ex){
     MessageBox.Show(this,(Index==1?"Controllo aggiornamenti plugin non riuscito: ":"Plugin update check failed: ")+ex.Message,"DJ RUSSO FX HUB",MessageBoxButton.OK,MessageBoxImage.Warning);
   }finally{PluginUpdatesButton.IsEnabled=true;}
 }
 private void SelectedProductInstall_Click(object sender,RoutedEventArgs e){
  if(selectedProductId=="meter-x"){MeterImport_Click(sender,e);return;}
  if(IsShellProduct(selectedProductId)){InstallSelectedShell();return;}
 }
 private void InstallSelectedShell(){
  if(ShellInstaller.GetState()!=ShellInstaller.State.Missing || !ShellInstaller.HasExpectedPayload())return;
  if(MessageBox.Show(this,Index==1?"Installare FX SHELL con Sidechain X, Reverse FX e Stutter FX? Chiudi FL Studio.":"Install FX SHELL including Sidechain X, Reverse FX and Stutter FX? Close FL Studio.","FX SHELL",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  int result=RunElevated(ShellInstaller.InstallArgument);
  if(result==0){try{ShellInstaller.SaveReceipt();}catch(Exception ex){MessageBox.Show(this,ex.Message,"FX SHELL");}}
  else MessageBox.Show(this,"FX SHELL: "+T(result==9?"cancelled":"ioError")+" ("+result+")","FX SHELL");
  Refresh();
 }
 private void SelectedProductRemove_Click(object sender,RoutedEventArgs e){
  if(selectedProductId=="meter-x"){MeterRemove_Click(sender,e);return;}
  if(!IsShellProduct(selectedProductId) || ShellInstaller.GetState()!=ShellInstaller.State.Managed)return;
  if(MessageBox.Show(this,Index==1?"Rimuovere FX SHELL? Verranno rimossi Sidechain X, Reverse FX e Stutter FX. Chiudi FL Studio.":"Remove FX SHELL? This removes Sidechain X, Reverse FX and Stutter FX. Close FL Studio.","FX SHELL",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
  int result=RunElevated(ShellInstaller.RemoveArgument);
  if(result==0){try{ShellInstaller.DeleteReceipt();}catch(Exception ex){MessageBox.Show(this,ex.Message,"FX SHELL");}}
  else MessageBox.Show(this,"FX SHELL: "+T(result==9?"cancelled":"ioError")+" ("+result+")","FX SHELL");
  Refresh();
 }
 private void SelectedProductLaunch_Click(object sender,RoutedEventArgs e){
  if(selectedProductId=="meter-x")MeterLaunch_Click(sender,e);
 }
 private void UpdateMeterUi(){ /* METER X controls live inside its existing card. */ }
 private void MeterImport_Click(object sender,RoutedEventArgs e){
  if(File.Exists(MeterExePath)){MessageBox.Show(this,Index==1?"METER X è già installato.":"METER X is already installed.","METER X");return;}
  InstallMeterFile(out var detail);
  Refresh();
  MessageBox.Show(this,detail,"METER X",MessageBoxButton.OK,MessageBoxImage.Information);
 }
 private void MeterLaunch_Click(object sender,RoutedEventArgs e){
  try{if(File.Exists(MeterExePath))Process.Start(new ProcessStartInfo{FileName=MeterExePath,WorkingDirectory=Path.GetDirectoryName(MeterExePath)!,UseShellExecute=true});}
  catch(Exception ex){MessageBox.Show(this,ex.Message,"METER X",MessageBoxButton.OK,MessageBoxImage.Warning);}
 }
 private void MeterRemove_Click(object sender,RoutedEventArgs e){
  if(!File.Exists(MeterExePath))return;
  var msg=Index==1?"Rimuovere solo la copia di METER X importata nell'HUB?":"Remove only the HUB-imported METER X copy?";
  if(MessageBox.Show(this,msg,"METER X",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
  try{File.Delete(MeterExePath);Refresh();}catch(Exception ex){MessageBox.Show(this,ex.Message,"METER X",MessageBoxButton.OK,MessageBoxImage.Warning);}
 }

 private void CloseScan_Click(object sender,RoutedEventArgs e) => ScanResultsPanel.Visibility=Visibility.Collapsed;
 private static readonly string[] ChipNames = { "Neon Wobble", "Ghoststep Rising", "Arcade Drop", "Future Skyline", "Midnight Trance", "Laser Bounce", "Pixel Rush", "Melodic Echo", "Bass Reactor", "Neon Horizon", "Glitch Runner", "Cyber Dreams", "Violet Drop", "Electric City", "Chiptune Anthem", "Retro Rave", "Wobble Reactor", "Starlight Bass", "Digital Hearts", "Ghostly Arps", "Nightdrive X", "Crystal Drop", "Voltage Rush", "Final Level" , "Neon Ghost Theme", "Cyber City", "Pixel Adventure", "Midnight Drive", "Arcade Rush", "Digital Dreams", "Space Explorer", "Bass Pulse", "Crystal Melody", "Retro Racer", "Future World", "DJ Russo Intro", "Moonlit Circuit", "Prism Runner", "Quantum Sunset", "Electric Aurora"};
 private readonly System.Windows.Threading.DispatcherTimer waveTimer=new(){Interval=TimeSpan.FromMilliseconds(50)};
 private readonly List<System.Windows.Shapes.Rectangle> waveBars=new();
 private double[] waveEnvelope=Array.Empty<double>();
 private void MiniWave_SizeChanged(object sender,SizeChangedEventArgs e)=>BuildMiniWave();
 private void BuildMiniWave(){
  if(MiniWaveCanvas==null)return;
  MiniWaveCanvas.Children.Clear();waveBars.Clear();
  int count=Math.Max(8,(int)(MiniWaveCanvas.ActualWidth/5));
  for(int i=0;i<count;i++){
   var bar=new System.Windows.Shapes.Rectangle{Width=2.7,Height=2,RadiusX=1.3,RadiusY=1.3,Fill=new SolidColorBrush((Color)ColorConverter.ConvertFromString(i%3==0?"#E756FA":"#48ECFF"))};
   System.Windows.Controls.Canvas.SetLeft(bar,i*5);System.Windows.Controls.Canvas.SetTop(bar,23);
   MiniWaveCanvas.Children.Add(bar);waveBars.Add(bar);
  }
 }
 private void UpdateMiniWave(){
  if(waveBars.Count==0)return;
  var pos=chipPlayer.Position.TotalSeconds;
  int center=(int)(pos/.12);
  for(int i=0;i<waveBars.Count;i++){
   int sample=center+(i-waveBars.Count/2)*2;
   double amp=(!chipPaused && !chipMuted && sample>=0 && sample<waveEnvelope.Length)?waveEnvelope[sample]:0.035;
   double h=Math.Max(2,Math.Min(44,amp*42));
   waveBars[i].Height=h;System.Windows.Controls.Canvas.SetTop(waveBars[i],(48-h)/2);
  }
 }
 private void LoadWaveEnvelope(){
  try{var f=Path.Combine(AppContext.BaseDirectory,"ChipMusic",$"ghost-chip-{chipIndex+1:00}.json");waveEnvelope=JsonSerializer.Deserialize<double[]>(File.ReadAllText(f))??Array.Empty<double>();}
  catch{waveEnvelope=Array.Empty<double>();}
 }
 private readonly MediaPlayer chipPlayer=new();
 private readonly System.Windows.Threading.DispatcherTimer chipTimer=new();
 private int chipIndex=0; private bool chipPaused=false; private bool chipMuted=false;
 private void SetChipPlaying(bool playing){ChipPauseIcon.Visibility=playing?Visibility.Visible:Visibility.Collapsed;ChipResumeIcon.Visibility=playing?Visibility.Collapsed:Visibility.Visible;}
 private void InitChipPlayer()
 {
   waveTimer.Tick+=(_,_)=>{if(WindowState!=WindowState.Minimized && IsVisible && !chipPaused)UpdateMiniWave();};
   StateChanged+=(_,_)=>{if(WindowState==WindowState.Minimized)waveTimer.Stop();else if(IsVisible)waveTimer.Start();};
   IsVisibleChanged+=(_,_)=>{if(IsVisible && WindowState!=WindowState.Minimized)waveTimer.Start();else waveTimer.Stop();};
   waveTimer.Start();BuildMiniWave();
   chipPlayer.MediaEnded += (_,_)=>NextChip();
   chipPlayer.MediaFailed += (_,_)=>{chipTimer.Stop();SetChipPlaying(false);};
   chipTimer.Interval=TimeSpan.FromMinutes(5); // MediaEnded advances at actual file duration; timer is a fallback.
   chipTimer.Tick += (_,_)=>NextChip();
   chipPlayer.Volume=0.10;
   PlayChip();
   Closed += (_,_)=>{waveTimer.Stop();chipTimer.Stop();chipPlayer.Stop();chipPlayer.Close();};
 }
 private void PlayChip()
 {
   string file=Path.Combine(AppContext.BaseDirectory,"ChipMusic",$"ghost-chip-{chipIndex+1:00}.wav");
   if(!File.Exists(file)){ChipTrackLabel.Text="CHIP AUDIO UNAVAILABLE";return;}
   LoadWaveEnvelope();chipPlayer.Open(new Uri(file));chipPlayer.Volume=chipMuted?0:ChipVolumeSlider.Value/100.0;
   chipPlayer.Play();chipPaused=false;SetChipPlaying(true);
   ChipTrackLabel.Text=$"{ChipNames[chipIndex]}  •  {chipIndex+1:00}/{ChipNames.Length:00}";chipTimer.Stop();chipTimer.Start();
 }
 private void NextChip(){chipIndex=(chipIndex+1)%ChipNames.Length;PlayChip();}
 private void ChipNext_Click(object sender,RoutedEventArgs e)=>NextChip();
 private void ChipPlay_Click(object sender,RoutedEventArgs e)
 {
   if(chipPaused){chipPlayer.Play();chipTimer.Start();SetChipPlaying(true);}
   else{chipPlayer.Pause();chipTimer.Stop();SetChipPlaying(false);}
   chipPaused=!chipPaused;
 }
 private void ChipMute_Click(object sender,RoutedEventArgs e)
 {chipMuted=!chipMuted;chipPlayer.Volume=chipMuted?0:ChipVolumeSlider.Value/100.0;ChipSoundWaves.Visibility=chipMuted?Visibility.Collapsed:Visibility.Visible;ChipMutedMark.Visibility=chipMuted?Visibility.Visible:Visibility.Collapsed;}
 private void ChipVolume_Changed(object sender,RoutedPropertyChangedEventArgs<double> e)
 {if(chipPlayer!=null)chipPlayer.Volume=chipMuted?0:e.NewValue/100.0;}
 private void Scan_Click(object sender,RoutedEventArgs e)
 {
     // Keep the scan report visible in the main window instead of relying on a transient dialog.
     ScanResultsPanel.Visibility=Visibility.Visible;
     ScanResultsHeading.Text=Index==1?"Rilevamento VST3 e METER X in corso…":"Detecting VST3 and METER X…";
     ScanResultsDetails.Text="";
     ScanResultsPath.Text="";
     try
     {
         var state=ShellInstaller.GetState();
         bool verified=state is ShellInstaller.State.Managed or ShellInstaller.State.ExternalSameBinary;
         var released=plugins.Where(p=>p.id!="meter-x" && !string.Equals(p.status,"Planned",StringComparison.OrdinalIgnoreCase))
                             .Select(p=>p.id).ToHashSet(StringComparer.OrdinalIgnoreCase);
         var found=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
         foreach(var package in manifest.packages)
             if(package.id=="fx-shell" && package.detector=="bundled-shell-sha256" && verified)
                 foreach(var id in package.effectIds)
                     if(released.Contains(id))found.Add(id);
         // METER X is a standalone EXE, not a VST3. Count it separately and
         // report its own installation state without implying a DAW scan.
         bool meterInstalled=File.Exists(MeterExePath);
         int count=found.Count+(meterInstalled?1:0);
         int total=released.Count+(plugins.Any(p=>p.id=="meter-x")?1:0);
         string headline=Index switch {
             1=>$"SOFTWARE RILEVATI: {count} / {total}",
             2=>$"SOFTWARE DETECTADO: {count} / {total}",
             3=>$"LOGICIELS DÉTECTÉS : {count} / {total}",
             4=>$"ERKANNTE SOFTWARE: {count} / {total}",
             _=>$"SOFTWARE DETECTED: {count} / {total}"
         };
         var names=plugins.Where(p=>found.Contains(p.id)).Select(p=>p.name).ToArray();
         string vstDetail=names.Length>0
             ? (Index==1?"FX SHELL (file VST3 rilevato): ":Index==2?"FX SHELL (archivo VST3 detectado): ":Index==3?"FX SHELL (fichier VST3 détecté) : ":Index==4?"FX SHELL (VST3-Datei erkannt): ":"FX SHELL (VST3 file detected): ")+string.Join(", ",names)
             : (Index==1?"FX SHELL: non rilevato.":Index==2?"FX SHELL: no detectado.":Index==3?"FX SHELL : non détecté.":Index==4?"FX SHELL: nicht erkannt.":"FX SHELL: not detected.");
         string meterDetail=meterInstalled
             ? (Index==1?"METER X: INSTALLATO (EXE trovato).":Index==2?"METER X: INSTALADO (EXE encontrado).":Index==3?"METER X : INSTALLÉ (EXE trouvé).":Index==4?"METER X: INSTALLIERT (EXE gefunden).":"METER X: INSTALLED (EXE found).")
             : (Index==1?"METER X: NON INSTALLATO.":Index==2?"METER X: NO INSTALADO.":Index==3?"METER X : NON INSTALLÉ.":Index==4?"METER X: NICHT INSTALLIERT.":"METER X: NOT INSTALLED.");
         string detail=vstDetail+"\n"+meterDetail;
         if(state==ShellInstaller.State.Conflict || state==ShellInstaller.State.ModifiedManaged)
             detail+="\n"+T(state==ShellInstaller.State.ModifiedManaged?"shellModified":"shellOther");
         Refresh();
         ScanResultsHeading.Text=headline;
         ScanResultsDetails.Text=detail;
         ScanResultsPath.Text=ShellInstaller.InstalledPath+"\n"+MeterExePath;
         ShellSectionStatus.Text=headline;
         StatusText.Text=headline;
         ScanResultsPanel.BorderBrush=(System.Windows.Media.Brush)FindResource("AccentBrush");

     }
     catch(Exception ex)
     {
         ScanResultsHeading.Text=Index==1?"ERRORE DURANTE LA SCANSIONE":"SCAN ERROR";
         ScanResultsDetails.Text=ex.Message;
         ScanResultsPath.Text=ShellInstaller.InstalledPath+"\n"+MeterExePath;
         MessageBox.Show(this,ex.ToString(),"DJ RUSSO FX HUB v0.6.1 — SCAN ERROR",MessageBoxButton.OK,MessageBoxImage.Error);
     }
 }


 private static readonly string[] GuideTexts = {
 """ 
 DJ RUSSO FX HUB — USER GUIDE

 ABOUT
 FX HUB is a Windows application for managing DJ RUSSO audio tools.
 The FX SHELL package contains three VST3 effects: Sidechain X, Reverse FX and Stutter FX.

 INSTALLING VST3 EFFECTS
 1. Close your DAW.
 2. Open FX HUB and select INSTALL FX SHELL.
 3. Approve the Windows administrator prompt.
 4. The VST3 is copied to C:\Program Files\Common Files\VST3.
 5. Open your DAW and rescan VST3 plugins if needed.

 DAW SUPPORT
 FL Studio is the primary development and testing environment.
 Cubase, Ableton Live, REAPER and other Windows DAWs supporting VST3 may load the effects.
 Compatibility with each DAW/version is not yet guaranteed or fully tested.
 You do not need to install FL Studio to use the HUB.

 DETECTION AND REMOVAL
 'Detect installed plugins' checks the HUB's verified FX SHELL binary at its standard location;
 it does NOT check that a particular DAW has successfully loaded the plugins.
 Uninstall removes only the unmodified installation owned by this HUB.
 Back up your projects and close the DAW before changing plugins.

 METER X
 DJ RUSSO METER X is a separate portable Windows audio meter.
 It is not part of the FX SHELL VST3 installer in this version.
 Its planned HUB integration will follow testing. WASAPI loopback may not capture
 audio routed through exclusive ASIO drivers.

 TROUBLESHOOTING
 If effects are missing: rescan VST3 plugins in the DAW, check the standard folder,
 restart the DAW, and ensure you use a Windows x64 VST3 host.
 If installation fails: close the DAW and approve administrator access.
 A successful HUB installation does not guarantee DAW compatibility.

 CREDITS
 Developed by Giuseppe Russo — DJ RUSSO.
 """,
 """
 DJ RUSSO FX HUB — GUIDA UTENTE

 INFORMAZIONI
 FX HUB è un'applicazione Windows per gestire gli strumenti audio DJ RUSSO.
 FX SHELL contiene tre effetti VST3: Sidechain X, Reverse FX e Stutter FX.

 INSTALLAZIONE VST3
 1. Chiudi la DAW.
 2. Apri FX HUB e seleziona INSTALLA FX SHELL.
 3. Autorizza la richiesta amministratore di Windows.
 4. Il VST3 viene copiato in C:\Program Files\Common Files\VST3.
 5. Apri la DAW ed esegui una nuova scansione VST3, se necessario.

 COMPATIBILITÀ DAW
 FL Studio è l'ambiente principale di sviluppo e test.
 Cubase, Ableton Live, REAPER e altre DAW Windows che supportano VST3
 possono caricare gli effetti, ma non tutte le versioni sono state testate.
 Non occorre installare FL Studio per utilizzare l'HUB.

 RILEVAMENTO E DISINSTALLAZIONE
 'Rileva plugin installati' verifica il file FX SHELL nella cartella standard;
 NON conferma che una DAW specifica riesca ad aprire i plugin.
 La disinstallazione rimuove soltanto la copia integra gestita dall'HUB.
 Salva i progetti e chiudi la DAW prima di modificare i plugin.

 METER X
 DJ RUSSO METER X è un misuratore audio portable separato.
 In questa versione non fa parte dell'installer VST3 FX SHELL.
 L'integrazione nell'HUB arriverà dopo i test. Il loopback WASAPI potrebbe
 non acquisire audio instradato tramite driver ASIO esclusivi.

 RISOLUZIONE DEI PROBLEMI
 Se gli effetti non compaiono: ripeti la scansione VST3 nella DAW, verifica
 la cartella standard, riavvia la DAW e usa un host VST3 Windows x64.
 Se l'installazione fallisce: chiudi la DAW e autorizza l'amministratore.
 L'installazione riuscita nell'HUB non garantisce compatibilità con ogni DAW.

 CREDITI
 Sviluppato da Giuseppe Russo — DJ RUSSO.
 """,
 """
 DJ RUSSO FX HUB — GUÍA DEL USUARIO

 DESCRIPCIÓN
 FX HUB administra herramientas de audio DJ RUSSO en Windows.
 FX SHELL incluye tres efectos VST3: Sidechain X, Reverse FX y Stutter FX.

 INSTALACIÓN
 1. Cierra tu DAW.
 2. Abre FX HUB y pulsa INSTALAR FX SHELL.
 3. Acepta el permiso de administrador de Windows.
 4. El VST3 se copia en C:\Program Files\Common Files\VST3.
 5. Abre tu DAW y vuelve a analizar los plugins VST3 si es necesario.

 COMPATIBILIDAD
 FL Studio es el entorno principal de desarrollo y pruebas.
 Cubase, Ableton Live, REAPER y otros DAW de Windows compatibles con VST3
 pueden cargar los efectos; no se ha verificado cada versión.
 No es necesario tener FL Studio instalado.

 DETECCIÓN Y DESINSTALACIÓN
 La detección comprueba el archivo FX SHELL verificado en la carpeta estándar,
 no confirma que cada DAW pueda cargarlo.
 La desinstalación solo elimina el archivo intacto gestionado por HUB.
 Guarda tus proyectos y cierra el DAW antes de hacer cambios.

 METER X
 DJ RUSSO METER X es una aplicación portable independiente.
 Todavía no está integrada en el instalador FX SHELL.
 WASAPI loopback puede no capturar audio mediante ASIO exclusivo.

 SOLUCIÓN DE PROBLEMAS
 Si faltan efectos, vuelve a analizar VST3, revisa la carpeta y reinicia el DAW.
 Usa un host VST3 Windows x64. Para instalar, autoriza al administrador.
 La instalación no garantiza compatibilidad con todos los DAW.

 CRÉDITOS
 Desarrollado por Giuseppe Russo — DJ RUSSO.
 """,
 """
 DJ RUSSO FX HUB — GUIDE UTILISATEUR

 PRÉSENTATION
 FX HUB gère les outils audio DJ RUSSO sous Windows.
 FX SHELL contient trois effets VST3 : Sidechain X, Reverse FX et Stutter FX.

 INSTALLATION
 1. Fermez votre DAW.
 2. Ouvrez FX HUB et choisissez INSTALLER FX SHELL.
 3. Acceptez l'autorisation administrateur Windows.
 4. Le VST3 est copié dans C:\Program Files\Common Files\VST3.
 5. Relancez votre DAW et analysez les plugins VST3 si nécessaire.

 COMPATIBILITÉ
 FL Studio est l'environnement principal de développement et de test.
 Cubase, Ableton Live, REAPER et d'autres DAW Windows compatibles VST3
 peuvent charger les effets ; toutes les versions ne sont pas testées.
 FL Studio n'est pas requis pour utiliser HUB.

 DÉTECTION ET SUPPRESSION
 La détection vérifie le fichier FX SHELL dans le dossier standard,
 mais ne prouve pas qu'un DAW donné puisse charger les effets.
 La suppression concerne uniquement le fichier intact géré par HUB.
 Sauvegardez vos projets et fermez le DAW avant toute modification.

 METER X
 DJ RUSSO METER X est une application portable distincte.
 Elle n'est pas encore intégrée à l'installation FX SHELL.
 WASAPI loopback peut ne pas capter les sorties ASIO exclusives.

 DÉPANNAGE
 Relancez l'analyse VST3, vérifiez le dossier standard et redémarrez le DAW.
 Utilisez un hôte VST3 Windows x64 et autorisez l'administrateur.
 L'installation ne garantit pas la compatibilité avec chaque DAW.

 CRÉDITS
 Développé par Giuseppe Russo — DJ RUSSO.
 """,
 """
 DJ RUSSO FX HUB — BENUTZERHANDBUCH

 ÜBERBLICK
 FX HUB verwaltet DJ RUSSO Audiowerkzeuge unter Windows.
 FX SHELL enthält drei VST3-Effekte: Sidechain X, Reverse FX und Stutter FX.

 INSTALLATION
 1. Schließe die DAW.
 2. Öffne FX HUB und wähle FX SHELL INSTALLIEREN.
 3. Bestätige die Windows-Administratorabfrage.
 4. Die VST3-Datei wird nach C:\Program Files\Common Files\VST3 kopiert.
 5. Starte die DAW und scanne die VST3-Plugins bei Bedarf erneut.

 DAW-KOMPATIBILITÄT
 FL Studio ist die primäre Entwicklungs- und Testumgebung.
 Cubase, Ableton Live, REAPER und andere Windows-DAWs mit VST3-Unterstützung
 können die Effekte laden. Nicht alle Versionen wurden getestet.
 FL Studio ist für die Nutzung des HUB nicht erforderlich.

 ERKENNUNG UND ENTFERNUNG
 Die Erkennung prüft die verifizierte FX SHELL-Datei im Standardordner.
 Sie bestätigt NICHT, dass eine bestimmte DAW die Plugins laden kann.
 Entfernt wird nur die unveränderte, vom HUB verwaltete Installation.
 Projekte sichern und DAW vor Änderungen schließen.

 METER X
 DJ RUSSO METER X ist eine separate portable Windows-Anwendung.
 Sie ist noch nicht Teil des FX SHELL-Installers.
 WASAPI-Loopback erfasst möglicherweise kein exklusives ASIO-Audio.

 FEHLERBEHEBUNG
 Bei fehlenden Effekten VST3 erneut scannen, Ordner prüfen und DAW neu starten.
 Windows-x64-VST3-Host verwenden; Administratorrechte bestätigen.
 Erfolgreiche Installation garantiert keine Kompatibilität mit jeder DAW.

 CREDITS
 Entwickelt von Giuseppe Russo — DJ RUSSO.
 """
 };
 private void Guide_Click(object sender,RoutedEventArgs e)
 {
  var dialog=new Window {
   Title=T("guide")+" — "+this.Title,
   Owner=this,Width=780,Height=610,MinWidth=520,MinHeight=350,
   WindowStartupLocation=WindowStartupLocation.CenterOwner,
   WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.CanResize,
   Background=(Brush)FindResource("BgBrush"),Foreground=(Brush)FindResource("FgBrush"),
   ShowInTaskbar=false
  };
  var frame=new Border {
   BorderBrush=(Brush)FindResource("AccentBrush"),BorderThickness=new Thickness(1.5),
   Background=(Brush)FindResource("BgBrush"),CornerRadius=new CornerRadius(9)
  };
  var root=new DockPanel();
  var titleBar=new Grid {Height=48,Background=(Brush)FindResource("SidebarBrush")};
  titleBar.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
  titleBar.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
  var title=new TextBlock {Text="DJ RUSSO FX HUB  •  "+T("guide"),FontSize=13,
   FontWeight=FontWeights.Bold,Foreground=(Brush)FindResource("SecondaryBrush"),
   VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(18,0,8,0)};
  titleBar.Children.Add(title);
  var close=new Button {Content="✕",Width=42,Height=34,Margin=new Thickness(0,0,9,0),
   FontSize=16,FontWeight=FontWeights.Bold,Cursor=System.Windows.Input.Cursors.Hand,
   Foreground=(Brush)FindResource("FgBrush"),Background=(Brush)FindResource("PanelBrush"),
   BorderBrush=(Brush)FindResource("AccentBrush"),BorderThickness=new Thickness(1),
   VerticalAlignment=VerticalAlignment.Center};
  Grid.SetColumn(close,1);titleBar.Children.Add(close);
  close.Click+=(_,_)=>dialog.Close();
  titleBar.MouseLeftButtonDown+=(_,e)=>{
   if(e.OriginalSource is System.Windows.DependencyObject origin){
    for(var current=origin;current!=null;current=System.Windows.Media.VisualTreeHelper.GetParent(current)){
     if(current is Button)return;
     if(ReferenceEquals(current,titleBar))break;
    }
   }
   if(e.ClickCount==2){dialog.WindowState=dialog.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;return;}
   try{dialog.DragMove();}catch{}
  };
  DockPanel.SetDock(titleBar,Dock.Top);root.Children.Add(titleBar);
  var layout=new DockPanel {Margin=new Thickness(18)};
  var header=new TextBlock {Text=T("guide"),FontSize=21,FontWeight=FontWeights.Bold,
   Foreground=(Brush)FindResource("SecondaryBrush"),Margin=new Thickness(0,0,0,12)};
  DockPanel.SetDock(header,Dock.Top);layout.Children.Add(header);
  var content=new TextBox {
   Text=GuideTexts[Index],IsReadOnly=true,TextWrapping=TextWrapping.Wrap,
   VerticalScrollBarVisibility=ScrollBarVisibility.Auto,
   HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,
   Background=(Brush)FindResource("PanelBrush"),
   Foreground=(Brush)FindResource("FgBrush"),
   BorderBrush=(Brush)FindResource("AccentBrush"),
   FontFamily=new FontFamily("Segoe UI"),FontSize=14,
   Padding=new Thickness(14)
  };
  // Custom vertical scroll bar only inside the Guide, not on other HUB controls.
  content.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)]=(Style)FindResource("NeonGuideScrollBar");
  layout.Children.Add(content);root.Children.Add(layout);frame.Child=root;dialog.Content=frame;
  dialog.ShowDialog();
 }

 // No upload and no API key: look up the SHA-256 digest published by GitHub for this release's installer.
 private string? verifiedInstallerSha256;
 private void CopyInstallerHash_Click(object sender,RoutedEventArgs e)
 {
   if(verifiedInstallerSha256 is null)return;
   try{
     Clipboard.SetText(verifiedInstallerSha256);
     SecurityStatus.Text="SHA-256: "+verifiedInstallerSha256+" • "+new[]{"Copied","Copiato","Copiado","Copié","Kopiert"}[Index];
   }catch(Exception ex){SecurityStatus.Text=ex.Message;}
 }
 private async void VirusTotalReport_Click(object sender,RoutedEventArgs e)
 {
  VirusTotalReportButton.IsEnabled=false;
  verifiedInstallerSha256=null;
  CopyInstallerHashButton.IsEnabled=false;
  SecurityStatus.Text=Index==1?"Recupero impronta SHA-256 ufficiale...":"Retrieving official SHA-256 digest...";
  try{
   var current=typeof(MainWindow).Assembly.GetName().Version?.ToString(3)??"0.8.22";
   using var response=await UpdateClient.GetAsync("https://api.github.com/repos/djrusso1994/DJ-RUSSO-FX-HUB/releases/tags/v"+current);
   response.EnsureSuccessStatusCode();
   using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
   string? digest=null;
   foreach(var asset in document.RootElement.GetProperty("assets").EnumerateArray()){
    var name=asset.GetProperty("name").GetString()??"";
    if(!name.Equals("DJ_RUSSO_FX_HUB_Setup_v"+current+".exe",StringComparison.OrdinalIgnoreCase))continue;
    if(asset.TryGetProperty("digest",out var value) && value.ValueKind==JsonValueKind.String){
     var raw=value.GetString()??"";
     if(raw.StartsWith("sha256:",StringComparison.OrdinalIgnoreCase))digest=raw.Substring(7);
    }
    break;
   }
   if(digest==null || digest.Length!=64 || !digest.All(Uri.IsHexDigit)){
    SecurityStatus.Text=Index==1?"Impronta SHA-256 non disponibile nei metadati GitHub per questa versione. Nessuna scansione eseguita.":"SHA-256 digest is not available in GitHub metadata for this version. No scan performed.";
    return;
   }
   verifiedInstallerSha256=digest;
   CopyInstallerHashButton.IsEnabled=true;
   SecurityStatus.Text="SHA-256: "+digest+" — "+(Index==1?"VirusTotal: carica manualmente l’installer per analizzarlo. Nessun file viene inviato automaticamente.":"VirusTotal: manually upload the installer to scan it. No file is uploaded automatically.");
   Process.Start(new ProcessStartInfo("https://www.virustotal.com/gui/home/upload"){UseShellExecute=true});
  }catch(Exception ex){
   SecurityStatus.Text=(Index==1?"Impossibile aprire il rapporto: ":"Unable to open report: ")+ex.Message;
  }finally{VirusTotalReportButton.IsEnabled=true;}
 }
 private (Window window,TextBlock status,System.Windows.Controls.ProgressBar bar,TextBlock details,Button cancel) CreateUpdateProgressDialog(Action onCancel,Func<bool> isCancelled){
     var progressWindow=new Window{
       Title="DJ RUSSO FX HUB • UPDATE",Width=490,MinWidth=440,SizeToContent=SizeToContent.Height,
       WindowStartupLocation=WindowStartupLocation.CenterOwner,Owner=this,
       WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,
       Background=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14,19,44)),
       Foreground=System.Windows.Media.Brushes.White,ShowInTaskbar=false
     };
     // Custom Neon Ghost title bar: no native white Windows title strip.
     var progressRoot=new Border{
       BorderBrush=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(142,89,229)),
       BorderThickness=new Thickness(1.5),CornerRadius=new CornerRadius(10),
       Background=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14,19,44)),
       SnapsToDevicePixels=true
     };
     var progressStack=new StackPanel();
     var progressTitleBar=new Grid{Height=46,Background=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24,20,49))};
     progressTitleBar.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
     progressTitleBar.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
     var progressTitleText=new TextBlock{Text="DJ RUSSO FX HUB  •  UPDATE",FontSize=12,FontWeight=FontWeights.SemiBold,
       VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(20,0,0,0),
       Foreground=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(125,239,255))};
     progressTitleBar.Children.Add(progressTitleText);
     progressTitleBar.MouseLeftButtonDown+=(_,e)=>{if(e.ClickCount==1)try{progressWindow.DragMove();}catch(InvalidOperationException){}};
     progressStack.Children.Add(progressTitleBar);
     var progressLayout=new StackPanel{Margin=new Thickness(24,18,24,24)};
     var progressHeading=new TextBlock{Text=Index==1?"AGGIORNAMENTO IN CORSO":"UPDATING DJ RUSSO FX HUB",FontSize=19,FontWeight=FontWeights.Bold,Foreground=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(103,231,242))};
     var progressStatus=new TextBlock{Text=Index==1?"Connessione a GitHub...":"Connecting to GitHub...",Margin=new Thickness(0,17,0,9),FontSize=13};
     var progressBrush=new System.Windows.Media.LinearGradientBrush{
       StartPoint=new Point(0,0.5),EndPoint=new Point(1,0.5),
       GradientStops=new System.Windows.Media.GradientStopCollection{
        new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(128,79,228),0),
        new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(250,123,226),0.47),
        new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(86,224,249),0.62),
        new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(128,79,228),1)
       },RelativeTransform=new System.Windows.Media.TranslateTransform()
     };
     ((System.Windows.Media.TranslateTransform)progressBrush.RelativeTransform).BeginAnimation(
      System.Windows.Media.TranslateTransform.XProperty,
      new System.Windows.Media.Animation.DoubleAnimation{
       From=-0.5,To=0.5,Duration=new Duration(TimeSpan.FromSeconds(1.7)),
       AutoReverse=true,RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever
      });
     var progressBar=new System.Windows.Controls.ProgressBar{
      Height=16,Minimum=0,Maximum=100,IsIndeterminate=true,
      Background=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(24,26,55)),
      BorderBrush=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(111,79,202)),
      BorderThickness=new Thickness(1),Foreground=progressBrush};
     var progressDetails=new TextBlock{Text="",Margin=new Thickness(0,10,0,0),FontSize=12};
     progressLayout.Children.Add(progressHeading);
     progressLayout.Children.Add(progressStatus);
     progressLayout.Children.Add(progressBar);
     progressLayout.Children.Add(progressDetails);
     var cancelButton=new Button{Content=Index==1?"ANNULLA":"CANCEL",MinWidth=124,MinHeight=40,Padding=new Thickness(14,8,14,8),HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,18,0,0),Foreground=System.Windows.Media.Brushes.White,Background=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35,30,67)),BorderBrush=new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(169,91,246)),BorderThickness=new Thickness(1.5),FontWeight=FontWeights.SemiBold,Cursor=System.Windows.Input.Cursors.Hand};
     // Explicit template prevents the default Windows button theme from hiding the label.
     var cancelBorderFactory=new FrameworkElementFactory(typeof(Border));
     cancelBorderFactory.SetValue(Border.BackgroundProperty,new System.Windows.TemplateBindingExtension(Button.BackgroundProperty));
     cancelBorderFactory.SetValue(Border.BorderBrushProperty,new System.Windows.TemplateBindingExtension(Button.BorderBrushProperty));
     cancelBorderFactory.SetValue(Border.BorderThicknessProperty,new System.Windows.TemplateBindingExtension(Button.BorderThicknessProperty));
     cancelBorderFactory.SetValue(Border.CornerRadiusProperty,new CornerRadius(7));
     var cancelTextFactory=new FrameworkElementFactory(typeof(ContentPresenter));
     cancelTextFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center);
     cancelTextFactory.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center);
     cancelTextFactory.SetValue(ContentPresenter.RecognizesAccessKeyProperty,true);
     cancelBorderFactory.AppendChild(cancelTextFactory);
     cancelButton.Template=new ControlTemplate(typeof(Button)){VisualTree=cancelBorderFactory};
     var cancelBorder=cancelButton.BorderBrush;
     var cancelBackground=cancelButton.Background;
     cancelButton.MouseEnter+=(_,__)=>{cancelButton.BorderBrush=new SolidColorBrush(Color.FromRgb(99,234,255));cancelButton.Background=new SolidColorBrush(Color.FromRgb(50,43,90));};
     cancelButton.MouseLeave+=(_,__)=>{cancelButton.BorderBrush=cancelBorder;cancelButton.Background=cancelBackground;};
     cancelButton.Click+=(_,__)=>{cancelButton.IsEnabled=false;onCancel();};
     progressLayout.Children.Add(cancelButton);
     progressStack.Children.Add(progressLayout);
     progressRoot.Child=progressStack;
     progressWindow.Content=progressRoot;
     progressWindow.Closing+=(_,e)=>{if(!!isCancelled() && cancelButton.IsEnabled){e.Cancel=true;onCancel();cancelButton.IsEnabled=false;}};
     return (progressWindow,progressStatus,progressBar,progressDetails,cancelButton);
 }
 private void PreviewUpdateDialog_Click(object sender,RoutedEventArgs e){
   bool cancelled=false;
   var (window,status,bar,details,cancel)=CreateUpdateProgressDialog(()=>{cancelled=true;},()=>cancelled);
   window.Title="DJ RUSSO FX HUB • PREVIEW";
   status.Text=Index==1?"ANTEPRIMA — nessun download reale":"PREVIEW — no actual download";
   bar.IsIndeterminate=false;
   bar.Value=0;
   details.Text=Index==1?"Simulazione • nessun file scaricato":"Simulation • no files downloaded";
   var demoTimer=new System.Windows.Threading.DispatcherTimer{
    Interval=TimeSpan.FromMilliseconds(55)
   };
   demoTimer.Tick+=(_,_)=>{
    if(cancelled || !window.IsVisible){demoTimer.Stop();return;}
    var next=Math.Min(100,bar.Value+1.4);
    bar.Value=next;
    var percent=next.ToString("F0");
    details.Text=Index==1?"Simulazione: "+percent+"% • nessun file scaricato":"Simulation: "+percent+"% • no files downloaded";
    if(next>=100){
     demoTimer.Stop();
     status.Text=Index==1?"✓ ANTEPRIMA COMPLETATA — nessun file installato":"✓ PREVIEW COMPLETE — no files installed";
    }
   };
   window.Loaded+=(_,_)=>demoTimer.Start();
   window.Closed+=(_,_)=>demoTimer.Stop();
   cancel.Click+=(_,_)=>window.Close();
   window.ShowDialog();
 }
 // The button and settings panel always share the same current-view state.
 private void Settings_Click(object sender,RoutedEventArgs e)
 {
  bool open = SettingsPanel.Visibility != Visibility.Visible;
  SettingsPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
  SettingsButton.Tag = open ? "Selected" : null;
 }

}
