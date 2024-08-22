namespace Vali.Core;

public class SubdivisionWeights
{
    private static Dictionary<string, int> DK = new()
    {
        { "DK-81", 100 }, // Nordjylland
        { "DK-82", 140 }, // Midtjylland
        { "DK-83", 150 }, // Syddanmark
        { "DK-84", 60 }, // Hovedstaden
        { "DK-85", 130 }, // Sjaelland
    };

    private static Dictionary<string, int> NL = new()
    {
        { "NL-DR", 80 },
        { "NL-FL", 45 },
        { "NL-FR", 90 },
        { "NL-GE", 150 },
        { "NL-GR", 100 },
        { "NL-LI", 90 },
        { "NL-NB", 150 },
        { "NL-NH", 110 },
        { "NL-OV", 110 },
        { "NL-UT", 60 },
        { "NL-ZE", 55 },
        { "NL-ZH", 100 },
    };

    private static Dictionary<string, int> NZ = new()
    {
        { "NZ-AUK", 2700 },
        { "NZ-BOP", 3100 },
        { "NZ-CAN", 5300 },
        { "NZ-GIS", 2250 },
        { "NZ-HKB", 3100 },
        { "NZ-MBH", 2500 },
        { "NZ-MWT", 3850 },
        { "NZ-NSN", 350 },
        { "NZ-NTL", 3100 },
        { "NZ-OTA", 4050 },
        { "NZ-STL", 3950 },
        { "NZ-TAS", 2500 },
        { "NZ-TKI", 2550 },
        { "NZ-WGN", 2850 },
        { "NZ-WKO", 3950 },
        { "NZ-WTC", 3900 },
    };


    private static Dictionary<string, int> IT = new()
    {
        { "IT-21", 25387 }, // Piemonte
        { "IT-23", 3261 }, // Valle d'Aosta
        { "IT-25", 23864 }, // Lombardia
        { "IT-32", 13606 }, // Trentino-Alto Adige
        { "IT-34", 18345 }, // Veneto
        { "IT-36", 7924 }, // Friuli-Venezia Giulia
        { "IT-42", 5416 }, // Liguria
        { "IT-45", 22453 }, // Emilia-Romagna
        { "IT-52", 22987 }, // Toscana
        { "IT-55", 8464 }, // Umbria
        { "IT-57", 9401 }, // Marche
        { "IT-62", 17232 }, // Lazio
        { "IT-65", 10832 }, // Abruzzo
        { "IT-67", 4461 }, // Molise
        { "IT-72", 13671 }, // Campania
        { "IT-75", 19541 }, // Puglia
        { "IT-77", 10073 }, // Basilicata
        { "IT-78", 15222 }, // Calabria
        { "IT-82", 25832 }, // Sicilia
        { "IT-88", 24100 }, // Sardegna
    };

    private static Dictionary<string, int> FR = new()
    {
        { "FR-01", 868 }, // Ain
        { "FR-02", 1024 }, // Aisne
        { "FR-03", 1046 }, // Allier
        { "FR-04", 954 }, // Alpes-de-Haute-Provence
        { "FR-05", 722 }, // Hautes-Alpes
        { "FR-06", 755 }, // Alpes-Maritimes
        { "FR-07", 787 }, // Ardèche
        { "FR-08", 704 }, // Ardennes
        { "FR-09", 632 }, // Ariège
        { "FR-10", 809 }, // Aube
        { "FR-11", 860 }, // Aude
        { "FR-12", 1248 }, // Aveyron
        { "FR-13", 1070 }, // Bouches-du-Rhône
        { "FR-14", 900 }, // Calvados
        { "FR-15", 784 }, // Cantal
        { "FR-16", 815 }, // Charente
        { "FR-17", 999 }, // Charente-Maritime
        { "FR-18", 998 }, // Cher
        { "FR-19", 846 }, // Corrèze
        { "FR-21", 1246 }, // Côte-d'Or
        { "FR-22", 1085 }, // Côtes-d'Armor
        { "FR-23", 727 }, // Creuse
        { "FR-24", 1472 }, // Dordogne
        { "FR-25", 819 }, // Doubs
        { "FR-26", 945 }, // Drôme
        { "FR-27", 909 }, // Eure
        { "FR-28", 851 }, // Eure-et-Loir
        { "FR-29", 1220 }, // Finistère
        { "FR-2A", 543 }, // Corse-du-Sud
        { "FR-2B", 653 }, // Haute-Corse
        { "FR-30", 932 }, // Gard
        { "FR-31", 1128 }, // Haute-Garonne
        { "FR-32", 844 }, // Gers
        { "FR-33", 1757 }, // Gironde
        { "FR-34", 1089 }, // Hérault
        { "FR-35", 1142 }, // Ille-et-Vilaine
        { "FR-36", 898 }, // Indre
        { "FR-37", 949 }, // Indre-et-Loire
        { "FR-38", 1258 }, // Isère
        { "FR-39", 784 }, // Jura
        { "FR-40", 1244 }, // Landes
        { "FR-41", 894 }, // Loir-et-Cher
        { "FR-42", 774 }, // Loire
        { "FR-43", 679 }, // Haute-Loire
        { "FR-44", 1253 }, // Loire-Atlantique
        { "FR-45", 1086 }, // Loiret
        { "FR-46", 735 }, // Lot
        { "FR-47", 771 }, // Lot-et-Garonne
        { "FR-48", 677 }, // Lozère
        { "FR-49", 1108 }, // Maine-et-Loire
        { "FR-50", 996 }, // Manche
        { "FR-51", 1108 }, // Marne
        { "FR-52", 854 }, // Haute-Marne
        { "FR-53", 700 }, // Mayenne
        { "FR-54", 799 }, // Meurthe-et-Moselle
        { "FR-55", 794 }, // Meuse
        { "FR-56", 1111 }, // Morbihan
        { "FR-57", 1014 }, // Moselle
        { "FR-58", 915 }, // Nièvre
        { "FR-59", 1387 }, // Nord
        { "FR-60", 901 }, // Oise
        { "FR-61", 813 }, // Orne
        { "FR-62", 1189 }, // Pas-de-Calais
        { "FR-63", 1204 }, // Puy-de-Dôme
        { "FR-64", 1121 }, // Pyrénées-Atlantiques
        { "FR-65", 621 }, // Hautes-Pyrénées
        { "FR-66", 664 }, // Pyrénées-Orientales
        { "FR-69", 682 }, // Rhône
        { "FR-69M", 188 }, // Métropole de Lyon
        { "FR-6AE", 1494 }, // Alsace
        { "FR-70", 724 }, // Haute-Saône
        { "FR-71", 1207 }, // Saône-et-Loire
        { "FR-72", 1035 }, // Sarthe
        { "FR-73", 875 }, // Savoie
        { "FR-74", 752 }, // Haute-Savoie
        { "FR-75C", 370 }, // Paris
        { "FR-76", 1132 }, // Seine-Maritime
        { "FR-77", 1068 }, // Seine-et-Marne
        { "FR-78", 570 }, // Yvelines
        { "FR-79", 850 }, // Deux-Sèvres
        { "FR-80", 909 }, // Somme
        { "FR-81", 799 }, // Tarn
        { "FR-82", 534 }, // Tarn-et-Garonne
        { "FR-83", 982 }, // Var
        { "FR-84", 643 }, // Vaucluse
        { "FR-85", 990 }, // Vendée
        { "FR-86", 980 }, // Vienne
        { "FR-87", 802 }, // Haute-Vienne
        { "FR-88", 823 }, // Vosges
        { "FR-89", 1075 }, // Yonne
        { "FR-90", 105 }, // Territoire de Belfort
        { "FR-91", 522 }, // Essonne
        { "FR-92", 295 }, // Hauts-de-Seine
        { "FR-93", 312 }, // Seine-Saint-Denis
        { "FR-94", 276 }, // Val-de-Marne
        { "FR-95", 403 }, // Val-d'Oise
    };

    private static Dictionary<string, int> LU = new()
    {
        { "LU-CA", 1000 }, // Capellen
        { "LU-CL", 1300 }, // Clervaux
        { "LU-DI", 1000 }, // Diekirch
        { "LU-EC", 700 }, // Echternach
        { "LU-ES", 1300 }, // Esch-sur-Alzette
        { "LU-GR", 1000 }, // Grevenmacher
        { "LU-LU", 1200 }, // Luxembourg
        { "LU-ME", 900 }, // Mersch
        { "LU-RD", 1000 }, // Redange
        { "LU-RM", 300 }, // Remich
        { "LU-VD", 250 }, // Vianden
        { "LU-WI", 1000 }, // Wiltz
    };

    private static Dictionary<string, int> BE = new()
    {
        { "BE-VAN", 1076 }, // Antwerpen
        { "BE-VBR", 793 }, // Vlaams-Brabant
        { "BE-VLI", 839 }, // Limburg
        { "BE-VOV", 1104 }, // Oost-Vlaanderen
        { "BE-VWV", 1107 }, // West-Vlaanderen
        { "BE-WBR", 382 }, // Brabant wallon
        { "BE-WHT", 1312 }, // Hainaut
        { "BE-WLG", 1282 }, // Liege
        { "BE-WLX", 819 }, // Luxembourg
        { "BE-WNA", 1140 }, // Namurc
        { "BE-BRU", 80 }, // Brussels
    };

    private static Dictionary<string, int> AU = new()
    {
        { "AU-ACT", 600 }, // Australian Capital Territory
        { "AU-NSW", 8735 }, // New South Wales
        { "AU-NT", 4366 }, // Northern Territory
        { "AU-QLD", 8735 }, // Queensland
        { "AU-SA",  8735 }, // South Australia
        { "AU-TAS", 4850 }, // Tasmania
        { "AU-VIC", 8735 }, // Victoria
        { "AU-WA",  8735 }, // Western Australia
        { "Jervis Bay Territory", 1 }, // Jervis Bay Territory
    };

    private static Dictionary<string, int> NO = new()
    {
        { "NO-03", 270 }, // Oslo
        { "NO-11", 3300 }, // Rogaland
        { "NO-15", 3500 }, // More og Romsdal
        { "NO-18", 5800 }, // Nordland
        { "NO-21", 15 }, // Svalbard
        { "NO-31", 1000 }, // Østfold
        { "NO-32", 1150 }, // Akershus
        { "NO-33", 2650 }, // Buskerud
        { "NO-34", 6300 }, // Innlandet
        { "NO-39", 650 }, // Vestfold
        { "NO-40", 3150 }, // Telemark
        { "NO-42", 3800 }, // Agder
        { "NO-46", 5800 }, // Vestland
        { "NO-50", 6000 }, // Trondelag
        { "NO-55", 3650 }, // Troms
        { "NO-56", 3550 }, // Finnmark
    };

    private static Dictionary<string, int> SE = new()
    {
        { "SE-AB", 1000 }, // Stockholms lan
        { "SE-AC", 1500 }, // Vasterbottens lan
        { "SE-BD", 2000 }, // Norrbottens lan
        { "SE-C", 600 }, // Uppsala lan
        { "SE-D", 600 }, // Sodermanlands lan
        { "SE-E", 700 }, // Ostergotlands lan
        { "SE-F", 700 }, // Jonkopings lan
        { "SE-G", 700 }, // Kronobergs lan
        { "SE-H", 800 }, // Kalmar lan
        { "SE-I", 600 }, // Gotlands lan
        { "SE-K", 500 }, // Blekinge lan
        { "SE-M", 1000 }, // Skane lan
        { "SE-N", 800 }, // Hallands lan
        { "SE-O", 1100 }, // Vastra Gotalands lan
        { "SE-S", 1200 }, // Varmlands lan
        { "SE-T", 600 }, // Orebro lan
        { "SE-U", 500 }, // Vastmanlands lan
        { "SE-W", 1200 }, // Dalarnas lan
        { "SE-X", 1000 }, // Gavleborgs lan
        { "SE-Y", 1000 }, // Vasternorrlands lan
        { "SE-Z", 1100 }, // Jamtlands lan
    };

    private static Dictionary<string, int> HR = new()
    {
        { "HR-01", 510 }, // Zagrebacka zupanija
        { "HR-02", 210 }, // Krapinsko-zagorska zupanija
        { "HR-03", 510 }, // Sisacko-moslavacka zupanija
        { "HR-04", 410 }, // Karlovacka zupanija
        { "HR-05", 220 }, // Varazdinska zupanija
        { "HR-06", 230 }, // Koprivnicko-krizevacka zupanija
        { "HR-07", 400 }, // Bjelovarsko-bilogorska zupanija
        { "HR-08", 700 }, // Primorsko-goranska zupanija
        { "HR-09", 650 }, // Licko-senjska zupanija
        { "HR-10", 250 }, // Viroviticko-podravska zupanija
        { "HR-11", 240 }, // Pozesko-slavonska zupanija
        { "HR-12", 250 }, // Brodsko-posavska zupanija
        { "HR-13", 650 }, // Zadarska zupanija
        { "HR-14", 470 }, // Osjecko-baranjska zupanija
        { "HR-15", 520 }, // Sibensko-kninska zupanija
        { "HR-16", 300 }, // Vukovarsko-srijemska zupanija
        { "HR-17", 800 }, // Splitsko-dalmatinska zupanija
        { "HR-18", 600 }, // Istarska zupanija
        { "HR-19", 450 }, // Dubrovacko-neretvanska zupanija
        { "HR-20", 150 }, // Medimurska zupanija
        { "HR-21", 130 }, // Grad Zagreb
    };

    private static Dictionary<string, int> EE = new()
    {
        { "EE-37", 620 }, // Harjumaa
        { "Hiiumaa vald", 120 }, // Harjumaa
        { "EE-39", 80 }, // Hiiumaa
        { "EE-44", 350 }, // Ida-Virumaa
        { "EE-50", 300 }, // Jogevamaa
        { "EE-52", 290 }, // Jarvamaa
        { "EE-57", 265 }, // Laanemaa
        { "EE-59", 450 }, // Laane-Virumaa
        { "EE-64", 200 }, // Polvamaa
        { "EE-68", 570 }, // Parnumaa
        { "EE-70", 370 }, // Raplamaa
        { "EE-74", 360 }, // Saaremaa
        { "EE-79", 290 }, // Tartumaa
        { "EE-81", 220 }, // Valgamaa
        { "EE-84", 290 }, // Viljandimaa
        { "EE-87", 210 }, // Vorumaa
    };

    private static readonly Dictionary<string, int> US = new()
    {
        { "US-AL", 1800 },
        { "US-AK", 1300 },
        { "US-AZ", 2300 },
        { "US-AR", 1750 },
        { "US-CA", 4500 },
        { "US-CO", 2500 },
        { "US-CT", 860 },
        { "US-DE", 550 },
        { "US-FL", 2400 },
        { "US-GA", 2300 },
        { "US-HI", 1000 },
        { "US-ID", 2700 },
        { "US-IL", 2400 },
        { "US-IN", 1850 },
        { "US-IA", 1800 },
        { "US-KS", 1900 },
        { "US-KY", 1800 },
        { "US-LA", 1700 },
        { "US-ME", 1750 },
        { "US-MD", 1100 },
        { "US-MA", 1000 },
        { "US-MI", 2050 },
        { "US-MN", 2050 },
        { "US-MS", 1800 },
        { "US-MO", 1900 },
        { "US-MT", 2900 },
        { "US-NE", 2100 },
        { "US-NV", 1900 },
        { "US-NH", 900 },
        { "US-NJ", 1100 },
        { "US-NM", 2250 },
        { "US-NY", 3100 },
        { "US-NC", 2300 },
        { "US-ND", 2100 },
        { "US-OH", 2400 },
        { "US-OK", 2100 },
        { "US-OR", 2600 },
        { "US-PA", 2800 },
        { "US-RI", 450 },
        { "US-SC", 1600 },
        { "US-SD", 2200 },
        { "US-TN", 2200 },
        { "US-TX", 4000 },
        { "US-UT", 2300 },
        { "US-VT", 900 },
        { "US-VA", 2500 },
        { "US-WA", 2600 },
        { "US-WV", 1800 },
        { "US-WI", 2050 },
        { "US-WY", 2500 },
        { "US-DC", 15 },
    };

    private static Dictionary<string, int> MX = new()
    {
        { "MX-AGU", 1250 }, // Aguascalientes
        { "MX-BCN", 3450 }, // Baja California
        { "MX-BCS", 3450 }, // Baja California Sur
        { "MX-CAM", 3800 }, // Campeche
        { "MX-CHH", 4300 }, // Chihuahua
        { "MX-CHP", 4600 }, // Chiapas
        { "MX-CMX", 450 }, // Ciudad de Mexico
        { "MX-COA", 3800 }, // Coahuila de Zaragoza
        { "MX-COL", 1350 }, // Colima
        { "MX-DUR", 4200 }, // Durango
        { "MX-GRO", 3150 }, // Guerrero
        { "MX-GUA", 3050 }, // Guanajuato
        { "MX-HID", 2400 }, // Hidalgo
        { "MX-JAL", 4600 }, // Jalisco
        { "MX-MEX", 2600 }, // Mexico
        { "MX-MIC", 3700 }, // Michoacan de Ocampo
        { "MX-MOR", 1300 }, // Morelos
        { "MX-NAY", 2500 }, // Nayarit
        { "MX-NLE", 3900 }, // Nuevo Leon
        { "MX-OAX", 4700 }, // Oaxaca
        { "MX-PUE", 3700 }, // Puebla
        { "MX-QUE", 1800 }, // Queretaro
        { "MX-ROO", 4100 }, // Quintana Roo
        { "MX-SIN", 3500 }, // Sinaloa
        { "MX-SLP", 4100 }, // San Luis Potosi
        { "MX-SON", 4200 }, // Sonora
        { "MX-TAB", 3200 }, // Tabasco
        { "MX-TAM", 4000 }, // Tamaulipas
        { "MX-TLA", 1200 }, // Tlaxcala
        { "MX-VER", 5000 }, // Veracruz de Ignacio de la Llave
        { "MX-YUC", 4000 }, // Yucatan
        { "MX-ZAC", 3800 }, // Zacatecas
    };

    private static readonly Dictionary<string, int> PR = new()
    {
        { "US-PR", 1250 }, // Puerto Rico
    };

    private static readonly Dictionary<string, int> DO = new()
    {
        { "DO-01", 143 }, // Distrito Nacional (Santo Domingo)
        { "DO-13", 2 }, // La Vega
        { "DO-25", 330 }, // Santiago
        { "DO-32", 424 }, // Santo Domingo
    };

    private static Dictionary<string, int> CW = new()
    {
        { "NL-CW", 1250 }, // Curacao
    };

    private static Dictionary<string, int> PA = new()
    {
        { "PA-1", 500 }, // Bocas del Toro
        { "PA-10", 1200 }, //
        { "PA-2", 1500 }, // Cocle
        { "PA-3", 700 }, // Colon
        { "PA-4", 2000 }, // Chiriqui
        { "PA-5", 40 }, // Darien
        { "PA-6", 600 }, // Herrera
        { "PA-7", 700 }, // Los Santos
        { "PA-8", 2500 }, // Panama
        { "PA-9", 1500 }, // Veraguas
        { "PA-NB", 400 }, // Ngobe-Bugle
        { "PA-NT", 10 }, //
    };

    private static Dictionary<string, int> CO = new()
    {
        { "CO-AMA", 0 }, // Amazonas
        { "CO-ANT", 4335 }, // Antioquia
        { "CO-ARA", 1186 }, // Arauca
        { "CO-ATL", 1717 }, // Atlantico
        { "CO-BOL", 1988 }, // Bolivar
        { "CO-BOY", 2569 }, // Boyaca
        { "CO-CAL", 2024 }, // Caldas
        { "CO-CAQ", 1380 }, // Caqueta
        { "CO-CAS", 3299 }, // Casanare
        { "CO-CAU", 3017 }, // Cauca
        { "CO-CES", 1918 }, // Cesar
        { "CO-CHO", 1717 }, // Choco
        { "CO-COR", 2280 }, // Cordoba
        { "CO-CUN", 4010 }, // Cundinamarca
        { "CO-DC", 1004 }, // Distrito Capital de Bogota
        { "CO-GUA", 0 }, // Guainia
        { "CO-GUV", 360 }, // Guaviare
        { "CO-HUI", 2479 }, // Huila
        { "CO-LAG", 2208 }, // La Guajira
        { "CO-MAG", 2354 }, // Magdalena
        { "CO-MET", 4191 }, // Meta
        { "CO-NAR", 3514 }, // Narino
        { "CO-NSA", 2501 }, // Norte de Santander
        { "CO-PUT", 1610 }, // Putumayo
        { "CO-QUI", 1186 }, // Quindio
        { "CO-RIS", 1610 }, // Risaralda
        { "CO-SAN", 3203 }, // Santander
        { "CO-SAP", 0 },   // San Andrés
        { "CO-SUC", 1710 }, // Sucre
        { "CO-TOL", 2673 }, // Tolima
        { "CO-VAC", 3226 }, // Valle del Cauca
        { "CO-VAU", 0 }, // Vaupes
        { "CO-VID", 110 }, // Vichada
    };

    private static Dictionary<string, int> EC = new()
    {
        { "EC-A", 1206 }, // Azuay
        { "EC-B", 870 }, // Bolivar
        { "EC-C", 884 }, // Carchi
        { "EC-D", 1450 }, // Orellana
        { "EC-E", 1440 }, // Esmeraldas
        { "EC-F", 871 }, // Canar
        { "EC-G", 2160 }, // Guayas
        { "EC-H", 1156 }, // Chimborazo
        { "EC-I", 1027 }, // Imbabura
        { "EC-L", 1934 }, // Loja
        { "EC-M", 2478 }, // Manabi
        { "EC-N", 1342 }, // Napo
        { "EC-O", 1488 }, // El Oro
        { "EC-P", 2067 }, // Pichincha
        { "EC-R", 1262 }, // Los Rios
        { "EC-S", 1144 }, // Morona Santiago
        { "EC-SD", 1301 }, // Santo Domingo de los Tsachilas
        { "EC-SE", 932 }, // Santa Elena
        { "EC-T", 1013 }, // Tungurahua
        { "EC-U", 1618 }, // Sucumbios
        { "EC-X", 1139 }, // Cotopaxi
        { "EC-Y", 1486 }, // Pastaza
        { "EC-Z", 256 }, // Zamora Chinchipe
    };

    private static Dictionary<string, int> PE = new()
    {
        { "PE-AMA", 1150 }, // Amazonas
        { "PE-ANC", 2150 }, // Ancash
        { "PE-APU", 1900 }, // Apurimac
        { "PE-ARE", 3000 }, // Arequipa
        { "PE-AYA", 1950 }, // Ayacucho
        { "PE-CAJ", 2650 }, // Cajamarca
        { "PE-CAL", 100 }, // El Callao
        { "PE-CUS", 2550 }, // Cusco
        { "PE-HUC", 1300 }, // Huanuco
        { "PE-HUV", 2100 }, // Huancavelica
        { "PE-ICA", 2150 }, // Ica
        { "PE-JUN", 2700 }, // Junin
        { "PE-LAL", 2550 }, // La Libertad
        { "PE-LAM", 2300 }, // Lambayeque
        { "PE-LIM", 2900 }, // Lima
        { "PE-LOR", 1050 }, // Loreto
        { "PE-MDD", 1500 }, // Madre de Dios
        { "PE-MOQ", 1750 }, // Moquegua
        { "PE-PAS", 1600 }, // Pasco
        { "PE-PIU", 2700 }, // Piura
        { "PE-PUN", 3400 }, // Puno
        { "PE-SAM", 1950 }, // San Martin
        { "PE-TAC", 2350 }, // Tacna
        { "PE-TUM", 1050 }, // Tumbes
        { "PE-UCA", 1200 }, // Ucayali
    };

    private static readonly Dictionary<string, int> BO = new()
    {
        { "BO-C", 1288 }, // Cochabamba
        { "BO-H", 622 }, // Chuquisaca
        { "BO-L", 1158 }, // La Paz
        { "BO-O", 632 }, // Oruro
        { "BO-P", 651 }, // Potosi
        { "BO-S", 1840 }, // Santa Cruz
        { "BO-T", 20 }, // Tarija
    };

    private static readonly Dictionary<string, int> CL = new()
    {
        { "CL-AI", 950 }, // Aisen del General Carlos Ibanez del Campo
        { "CL-AN", 900 }, // Antofagasta
        { "CL-AP", 300 }, // Arica y Parinacota
        { "CL-AR", 400 }, // La Araucania
        { "CL-AT", 700 }, // Atacama
        { "CL-BI", 250 }, // Biobio
        { "CL-CO", 600 }, // Coquimbo
        { "CL-LI", 250 }, // Libertador General Bernardo O'Higgins
        { "CL-LL", 800 }, // Los Lagos
        { "CL-LR", 300 }, // Los Rios
        { "CL-MA", 600 }, // Magallanes
        { "CL-ML", 450 }, // Maule
        { "CL-NB", 200 }, // Nuble
        { "CL-RM", 350 }, // Region Metropolitana de Santiago
        { "CL-TA", 500 }, // Tarapaca
        { "CL-VS", 450 }, // Valparaiso
    };

    private static readonly Dictionary<string, int> AR = new()
    {
        { "AR-A", 5061 }, // Salta
        { "AR-B", 7573 }, // Buenos Aires
        { "AR-C", 421 }, // Ciudad Autonoma de Buenos Aires
        { "AR-D", 4051 }, // San Luis
        { "AR-E", 4170 }, // Entre Rios
        { "AR-F", 4490 }, // La Rioja
        { "AR-G", 4580 }, // Santiago del Estero
        { "AR-H", 4516 }, // Chaco
        { "AR-J", 4504 }, // San Juan
        { "AR-K", 4485 }, // Catamarca
        { "AR-L", 4743 }, // La Pampa
        { "AR-M", 5356 }, // Mendoza
        { "AR-N", 3682 }, // Misiones
        { "AR-P", 3421 }, // Formosa
        { "AR-Q", 4974 }, // Neuquen
        { "AR-R", 6210 }, // Rio Negro
        { "AR-S", 4832 }, // Santa Fe
        { "AR-T", 3270 }, // Tucuman
        { "AR-U", 6577 }, // Chubut
        { "AR-V", 3170 }, // Tierra del Fuego
        { "AR-W", 4416 }, // Corrientes
        { "AR-X", 5620 }, // Cordoba
        { "AR-Y", 3770 }, // Jujuy
        { "AR-Z", 7132 }, // Santa Cruz
    };

    private static readonly Dictionary<string, int> BR = new()
    {
        { "BR-AC", 1888 }, // Acre
        { "BR-AL", 1839 }, // Alagoas
        { "BR-AM", 2096 }, // Amazonas
        { "BR-AP", 1679 }, // Amapa
        { "BR-BA", 7333 }, // Bahia
        { "BR-CE", 4038 }, // Ceará
        { "BR-DF", 250 }, // Distrito Federal
        { "BR-ES", 2289 }, // Espirito Santo
        { "BR-GO", 6423 }, // Goias
        { "BR-MA", 3626 }, // Maranhao
        { "BR-MG", 8894 }, // Minas Gerais
        { "BR-MS", 4760 }, // Mato Grosso do Sul
        { "BR-MT", 4754 }, // Mato Grosso
        { "BR-PA", 3567 }, // Para
        { "BR-PB", 2524 }, // Paraiba
        { "BR-PE", 3048 }, // Pernambuco
        { "BR-PI", 3610 }, // Piaui
        { "BR-PR", 5701 }, // Parana
        { "BR-RJ", 2433 }, // Rio de Janeiro
        { "BR-RN", 2519 }, // Rio Grande do Norte
        { "BR-RO", 2438 }, // Rondonia
        { "BR-RR", 2225 }, // Roraima
        { "BR-RS", 5990 }, // Rio Grande do Sul
        { "BR-SC", 3471 }, // Santa Catarina
        { "BR-SE", 1823 }, // Sergipe
        { "BR-SP", 6931 }, // Sao Paulo
        { "BR-TO", 3851 }, // Tocantins
    };

    private static readonly Dictionary<string, int> UY = new()
    {
        { "UY-AR", 250 }, // Artigas
        { "UY-CA", 800 }, // Canelones
        { "UY-CL", 700 }, // Cerro Largo
        { "UY-CO", 900 }, // Colonia
        { "UY-DU", 1000 }, // Durazno
        { "UY-FD", 700 }, // Florida
        { "UY-FS", 250 }, // Flores
        { "UY-LA", 1000 }, // Lavalleja
        { "UY-MA", 800 }, // Maldonado
        { "UY-MO", 80 }, // Montevideo
        { "UY-PA", 700 }, // Paysandu
        { "UY-RN", 700 }, // Rio Negro
        { "UY-RO", 1000 }, // Rocha
        { "UY-RV", 800 }, // Rivera
        { "UY-SA", 800 }, // Salto
        { "UY-SJ", 800 }, // San Jose
        { "UY-SO", 1000 }, // Soriano
        { "UY-TA", 1000 }, // Tacuarembo
        { "UY-TT", 800 }, // Treinta y Tres
    };

    private static readonly Dictionary<string, int> GT = new()
    {
        { "GT-01", 617 }, // Guatemala
        { "GT-02", 147 }, // El Progreso
        { "GT-03", 154 }, // Sacatepequez
        { "GT-04", 133 }, // Chimaltenango
        { "GT-05", 402 }, // Escuintla
        { "GT-06", 277 }, // Santa Rosa
        { "GT-07", 218 }, // Solola
        { "GT-08", 104 }, // Totonicapan
        { "GT-09", 256 }, // Quetzaltenango
        { "GT-10", 148 }, // Suchitepequez
        { "GT-11", 133 }, // Retalhuleu
        { "GT-12", 294 }, // San Marcos
        { "GT-13", 219 }, // Huehuetenango
        { "GT-14", 263 }, // Quiche
        { "GT-15", 170 }, // Baja Verapaz
        { "GT-16", 329 }, // Alta Verapaz
        { "GT-17", 401 }, // Peten
        { "GT-18", 268 }, // Izabal
        { "GT-19", 95 }, // Zacapa
        { "GT-20", 178 }, // Chiquimula
        { "GT-21", 202 }, // Jalapa
        { "GT-22", 235 }, // Jutiapa
    };

    private static Dictionary<string, int> ID = new()
    {
        { "ID-AC", 800 }, // Aceh
        { "ID-BA", 4000 }, // Bali
        { "ID-BB", 2300 }, // Kepulauan Bangka Belitung
        { "ID-BE", 2000 }, // Bengkulu
        { "ID-BT", 2300 }, // Banten
        { "ID-GO", 2000 }, // Gorontalo
        { "ID-JA", 2500 }, // Jambi
        { "ID-JB", 6200 }, // Jawa Barat
        { "ID-JI", 8400 }, // Jawa Timur
        { "ID-JK", 600 }, // Jakarta Raya
        { "ID-JT", 6200 }, // Jawa Tengah
        { "ID-KB", 4200 }, // Kalimantan Barat
        { "ID-KI", 3600 }, // Kalimantan Timur
        { "ID-KR", 1500 }, // Kepulauan Riau
        { "ID-KS", 3400 }, // Kalimantan Selatan
        { "ID-KT", 3000 }, // Kalimantan Tengah
        { "ID-KU", 1200 }, // Kalimantan Utara
        { "ID-LA", 4000 }, // Lampung
        { "ID-MU", 1400 }, // Maluku Utara
        { "ID-NB", 5400 }, // Nusa Tenggara Barat
        { "ID-NT", 7200 }, // Nusa Tenggara Timur
        { "ID-RI", 4100 }, // Riau
        { "ID-SA", 3300 }, // Sulawesi Utara
        { "ID-SB", 3900 }, // Sumatera Barat
        { "ID-SG", 3400 }, // Sulawesi Tenggara
        { "ID-SN", 5700 }, // Sulawesi Selatan
        { "ID-SR", 1400 }, // Sulawesi Barat
        { "ID-SS", 4100 }, // Sumatera Selatan
        { "ID-ST", 4700 }, // Sulawesi Tengah
        { "ID-SU", 6400 }, // Sumatera Utara
        { "ID-YO", 1000 }, // Yogyakarta
    };

    private static Dictionary<string, int> CA = new()
    {
        { "CA-AB", 5366 }, // Alberta
        { "CA-BC", 6706 }, // British Columbia
        { "CA-MB", 4471 }, // Manitoba
        { "CA-NB", 3219 }, // New Brunswick
        { "CA-NL", 4471 }, // Newfoundland and Labrador
        { "CA-NS", 3219 }, // Nova Scotia
        { "CA-NT", 824 }, // Northwest Territories
        { "CA-ON", 8585 }, // Ontario
        { "CA-PE", 1072 }, // Prince Edward Island
        { "CA-QC", 7512 }, // Quebec
        { "CA-SK", 4471 }, // Saskatchewan
        { "CA-YT", 2489 }, // Yukon
        { "CA-NU", 88 }, // Nunavut
    };

    private static Dictionary<string, int> GR = new()
    {
        { "GR-A", 1263 }, // Anatoliki Makedonia kai Thraki
        { "GR-B", 1675 }, // Kentriki Makedonia
        { "GR-C", 846 }, // Dytiki Makedonia
        { "GR-D", 852 }, // Ipeiros
        { "GR-E", 1281 }, // Thessalia
        { "GR-F", 540 }, // Ionia Nisia
        { "GR-G", 1115 }, // Dytiki Ellada
        { "GR-H", 1397 }, // Sterea Ellada
        { "GR-I", 946 }, // Attiki
        { "GR-J", 1476 }, // Peloponnisos
        { "GR-K", 354 }, // Voreio Aigaio
        { "GR-L", 486 }, // Notio Aigaio
        { "GR-M", 858 }, // Kriti
    };

    private static Dictionary<string, int> HU = new()
    {
        { "HU-BA", 911 }, // Baranya
        { "HU-BE", 1045 }, // Bekes
        { "HU-BK", 1615 }, // Bacs-Kiskun
        { "HU-BU", 1060 }, // Budapest
        { "HU-BZ", 1475 }, // Borsod-Abauj-Zemplen
        { "HU-CS", 856 }, // Csongrad-Csanad
        { "HU-FE", 879 }, // Fejer
        { "HU-GS", 1311 }, // Gyor-Moson-Sopron
        { "HU-HB", 1203 }, // Hajdu-Bihar
        { "HU-HE", 733 }, // Heves
        { "HU-JN", 1018 }, // Jasz-Nagykun-Szolnok
        { "HU-KE", 491 }, // Komarom-Esztergom
        { "HU-NO", 507 }, // Nograd
        { "HU-PE", 1542 }, // Pest
        { "HU-SO", 1126 }, // Somogy
        { "HU-SZ", 1185 }, // Szabolcs-Szatmar-Bereg
        { "HU-TO", 713 }, // Tolna
        { "HU-VA", 667 }, // Vas
        { "HU-VE", 893 }, // Veszprem
        { "HU-ZA", 770 }, // Zala
        { "Great Plain and North", 0 }, // Great Plain and North
        { "Central Hungary", 0 }, // Central Hungary
        { "Transdanubia", 0 }, // Transdanubia
    };

    private static Dictionary<string, int> IS = new()
    {
        { "IS-1", 814 }, // Hofudborgarsvaedi
        { "IS-2", 129 }, // Sudurnes
        { "IS-3", 720 }, // Vesturland
        { "IS-4", 693 }, // Vestfirdir
        { "IS-5", 600 }, // Nordurland vestra
        { "IS-6", 1100 }, // Nordurland eystra
        { "IS-7", 950 }, // Austurland
        { "IS-8", 1765 }, // Sudurland
    };

    private static Dictionary<string, int> FI = new()
    {
        { "FI-02", 359 }, // Etela-Karjala
        { "FI-03", 653 }, // Etela-Pohjanmaa
        { "FI-04", 815 }, // Etela-Savo
        { "FI-05", 952 }, // Kainuu
        { "FI-06", 336 }, // Kanta-Hame
        { "FI-07", 302 }, // Keski-Pohjanmaa
        { "FI-08", 926 }, // Keski-Suomi
        { "FI-09", 365 }, // Kymenlaakso
        { "FI-11", 839 }, // Pirkanmaa
        { "FI-12", 758 }, // Pohjanmaa
        { "FI-13", 961 }, // Pohjois-Karjala
        { "FI-14", 1823 }, // Pohjois-Pohjanmaa
        { "FI-15", 947 }, // Pohjois-Savo
        { "FI-16", 383 }, // Paijat-Hame
        { "FI-17", 566 }, // Satakunta
        { "FI-18", 1340 }, // Uusimaa
        { "FI-19", 999 }, // Varsinais-Suomi
        { "Mainland Finland", 2200 }, // Mainland Finland
        { "Åland Islands", 500 }, // Åland Islands
    };

    private static Dictionary<string, int> LV = new()
    {
        { "LV-002", 1396 }, // Aizkraukles novads
        { "LV-007", 1061 }, // Aluksnes novads
        { "LV-011", 243 }, // Adazu novads
        { "LV-015", 1214 }, // Balvu novads
        { "LV-016", 1405 }, // Bauskas novads
        { "LV-022", 1852 }, // Cesu novads
        { "LV-026", 1019 }, // Dobeles novads
        { "LV-033", 1164 }, // Gulbenes novads
        { "LV-041", 1035 }, // Jelgavas novads
        { "LV-042", 1830 }, // Jekabpils novads
        { "LV-047", 1392 }, // Kraslavas novads
        { "LV-050", 1581 }, // Kuldigas novads
        { "LV-052", 407 }, // Kekavas novads
        { "LV-054", 1498 }, // Limbazu novads
        { "LV-056", 388 }, // Livanu novads
        { "LV-058", 1466 }, // Ludzas novads
        { "LV-059", 1890 }, // Madonas novads
        { "LV-062", 190 }, // Marupes novads
        { "LV-067", 1296 }, // Ogres novads
        { "LV-068", 256 }, // Olaines novads
        { "LV-073", 855 }, // Preilu novads
        { "LV-077", 1686 }, // Rezeknes novads
        { "LV-080", 274 }, // Ropazu novads
        { "LV-087", 171 }, // Salaspils novads
        { "LV-088", 1321 }, // Saldus novads
        { "LV-089", 206 }, // Saulkrastu novads
        { "LV-091", 728 }, // Siguldas novads
        { "LV-094", 1111 }, // Smiltenes novads
        { "LV-097", 1738 }, // Talsu novads
        { "LV-099", 1207 }, // Tukuma novads
        { "LV-101", 541 }, // Valkas novads
        { "LV-102", 65 }, // Varaklanu novads
        { "LV-106", 1150 }, // Ventspils novads
        { "LV-111", 1523 }, // Augsdaugavas novads
        { "LV-112", 2148 }, // Dienvidkurzemes novads
        { "LV-113", 1872 }, // Valmieras novads
        { "LV-DGV", 293 }, // Daugavpils
        { "LV-JEL", 204 }, // Jelgava
        { "LV-JUR", 224 }, // Jurmala
        { "LV-LPX", 209 }, // Liepaja
        { "LV-REZ", 93 }, // Rēzekne
        { "LV-RIX", 2102 }, // Riga
        { "LV-VEN", 133 }, // Ventspils
    };

    private static Dictionary<string, int> LT = new()
    {
        { "LT-AL", 786 }, // Alytaus apskritis
        { "LT-KL", 848 }, // Klaipedos apskritis
        { "LT-KU", 1351 }, // Kauno apskritis
        { "LT-MR", 661 }, // Marijampoles apskritis
        { "LT-PN", 1144 }, // Panevezio apskritis
        { "LT-SA", 1265 }, // Siauliu apskritis
        { "LT-TA", 629 }, // Taurages apskritis
        { "LT-TE", 645 }, // Telsiu apskritis
        { "LT-UT", 1020 }, // Utenos apskritis
        { "LT-VL", 1651 }, // Vilniaus apskritis
    };

    private static Dictionary<string, int> PL = new()
    {
        { "PL-02", 1075 }, // Dolnoslaskie
        { "PL-04", 910 }, // Kujawsko-pomorskie
        { "PL-06", 1216 }, // Lubelskie
        { "PL-08", 652 }, // Lubuskie
        { "PL-10", 934 }, // Lodzkie
        { "PL-12", 927 }, // Malopolskie
        { "PL-14", 1870 }, // Mazowieckie
        { "PL-16", 457 }, // Opolskie
        { "PL-18", 952 }, // Podkarpackie
        { "PL-20", 933 }, // Podlaskie
        { "PL-22", 927 }, // Pomorskie
        { "PL-24", 863 }, // Slaskie
        { "PL-26", 580 }, // Swietokrzyskie
        { "PL-28", 1100 }, // Warminsko-mazurskie
        { "PL-30", 1526 }, // Wielkopolskie
        { "PL-32", 1078 }, // Zachodniopomorskie
    };

    private static Dictionary<string, int> SZ = new()
    {
        { "SZ-HH", 955 }, // Hhohho
        { "SZ-LU", 1199 }, // Lubombo
        { "SZ-MA", 1029 }, // Manzini
        { "SZ-SH", 818 }, // Shiselweni
    };

    private static Dictionary<string, int> AT = new()
    {
        { "AT-1", 415 }, // Burgenland
        { "AT-2", 945 }, // Karnten
        { "AT-3", 2025 }, // Niederosterreich
        { "AT-4", 1372 }, // Oberosterreich
        { "AT-5", 724 }, // Salzburg
        { "AT-6", 1701 }, // Steiermark
        { "AT-7", 1239 }, // Tirol
        { "AT-8", 309 }, // Vorarlberg
        { "AT-9", 269 }, // Wien
    };

    private static readonly Dictionary<string, int> ES = new()
    {
        { "ES-AN", 11600 }, // Andalucia
        { "ES-AR", 8300 }, // Aragon
        { "ES-AS", 3900 }, // Asturias, Principado de
        { "ES-CB", 3500 }, // Cantabria
        { "ES-CE", 400 }, // Ceuta
        { "ES-CL", 12500 }, // Castilla y Leon
        { "ES-CM", 10250 }, // Castilla-La Mancha
        { "ES-CN", 3000 }, // Canarias
        { "ES-CT", 7500 }, // Catalunya
        { "ES-EX", 7000 }, // Extremadura
        { "ES-GA", 6800 }, // Galicia
        { "ES-IB", 3000 }, // Illes Balears
        { "ES-MC", 4000 }, // Murcia, Región de
        { "ES-MD", 3500 }, // Madrid, Comunidad de
        { "ES-ML", 350 }, // Melilla
        { "ES-NC", 4000 }, // Navarra, Comunidad Foral de
        { "ES-PV", 3700 }, // Pais Vasco
        { "ES-RI", 2000 }, // La Rioja
        { "ES-VC", 5500 }, // Valenciana, Comunidad
    };

    private static readonly Dictionary<string, int> DE = new()
    {
        { "DE-BB", 7660 }, // Brandenburg
        { "DE-BE", 1373 }, // Berlin
        { "DE-BW", 10219 }, // Baden-Wurttemberg
        { "DE-BY", 17072 }, // Bayern
        { "DE-HB", 314 }, // Bremen
        { "DE-HE", 4122 }, // Hessen
        { "DE-HH", 765 }, // Hamburg
        { "DE-MV", 5897 }, // Mecklenburg-Vorpommern
        { "DE-NI", 13443 }, // Niedersachsen
        { "DE-NW", 12916 }, // Nordrhein-Westfalen
        { "DE-RP", 5699 }, // Rheinland-Pfalz
        { "DE-SH", 4580 }, // Schleswig-Holstein
        { "DE-SL", 909 }, // Saarland
        { "DE-SN", 5378 }, // Sachsen
        { "DE-ST", 5422 }, // Sachsen-Anhalt
        { "DE-TH", 4231 }, // Thuringen
    };

    private static readonly Dictionary<string, int> CZ = new()
    {
        { "CZ-10", 338 }, // Praha, Hlavní město
        { "CZ-20", 2011 }, // Stredocesky kraj
        { "CZ-31", 1707 }, // Jihocesky kraj
        { "CZ-32", 1235 }, // Plzensky kraj
        { "CZ-41", 549 }, // Karlovarsky kraj
        { "CZ-42", 954 }, // Ustecky kraj
        { "CZ-51", 560 }, // Liberecky kraj
        { "CZ-52", 813 }, // Kralovehradecky kraj
        { "CZ-53", 774 }, // Pardubicky kraj
        { "CZ-63", 1143 }, // Kraj Vysocina
        { "CZ-64", 1254 }, // Jihomoravsky kraj
        { "CZ-71", 913 }, // Olomoucky kraj
        { "CZ-72", 679 }, // Zlinsky kraj
        { "CZ-80", 1070 }, // Moravskoslezsky kraj
    };

    private static readonly Dictionary<string, int> SK = new()
    {
        { "SK-BC", 3950 }, // Banskobystricky kraj
        { "SK-BL", 1334 }, // Bratislavsky kraj
        { "SK-KI", 3127 }, // Kosicky kraj
        { "SK-NI", 3520 }, // Nitriansky kraj
        { "SK-PV", 3690 }, // Presovsky kraj
        { "SK-TA", 2596 }, // Trnavsky kraj
        { "SK-TC", 2366 }, // Trenciansky kraj
        { "SK-ZI", 2603 }, // Zilinsky kraj
    };

    private static readonly Dictionary<string, int> PT = new()
    {
        { "PT-01", 794 }, // Aveiro
        { "PT-02", 1852 }, // Beja
        { "PT-03", 801 }, // Braga
        { "PT-04", 1091 }, // Braganca
        { "PT-05", 1242 }, // Castelo Branco
        { "PT-06", 875 }, // Coimbra
        { "PT-07", 1344 }, // Evora
        { "PT-08", 1073 }, // Faro
        { "PT-09", 1035 }, // Guarda
        { "PT-10", 1006 }, // Leiria
        { "PT-11", 1134 }, // Lisboa
        { "PT-12", 1101 }, // Portalegre
        { "PT-13", 980 }, // Porto
        { "PT-14", 1398 }, // Santarem
        { "PT-15", 1110 }, // Setubal
        { "PT-16", 510 }, // Viana do Castelo
        { "PT-17", 884 }, // Vila Real
        { "PT-18", 1050 }, // Viseu
        { "PT-20", 495 }, // Regiao Autonoma dos Acores
        { "PT-30", 224 }, // Regiao Autonoma da Madeira
    };

    private static readonly Dictionary<string, int> RO = new()
    {
        { "RO-AB", 2007 }, // Alba
        { "RO-AG", 1108 }, // Arges
        { "RO-AR", 1379 }, // Arad
        { "RO-B", 579 }, // Bucuresti
        { "RO-BC", 1070 }, // Bacau
        { "RO-BH", 1455 }, // Bihor
        { "RO-BN", 953 }, // Bistrita-Nasaud
        { "RO-BR", 538 }, // Braila
        { "RO-BT", 984 }, // Botosani
        { "RO-BV", 954 }, // Brasov
        { "RO-BZ", 1004 }, // Buzau
        { "RO-CJ", 1408 }, // Cluj
        { "RO-CL", 745 }, // Calarasi
        { "RO-CS", 1396 }, // Caras-Severin
        { "RO-CT", 1055 }, // Constanta
        { "RO-CV", 551 }, // Covasna
        { "RO-DB", 698 }, // Dambovita
        { "RO-DJ", 1280 }, // Dolj
        { "RO-GJ", 838 }, // Gorj
        { "RO-GL", 708 }, // Galati
        { "RO-GR", 498 }, // Giurgiu
        { "RO-HD", 1385 }, // Hunedoara
        { "RO-HR", 1087 }, // Harghita
        { "RO-IF", 396 }, // Ilfov
        { "RO-IL", 612 }, // Ialomita
        { "RO-IS", 965 }, // Iasi
        { "RO-MH", 724 }, // Mehedinti
        { "RO-MM", 1078 }, // Maramures
        { "RO-MS", 1196 }, // Mures
        { "RO-NT", 1052 }, // Neamt
        { "RO-OT", 866 }, // Olt
        { "RO-PH", 948 }, // Prahova
        { "RO-SB", 1004 }, // Sibiu
        { "RO-SJ", 703 }, // Salaj
        { "RO-SM", 750 }, // Satu Mare
        { "RO-SV", 1366 }, // Suceava
        { "RO-TL", 1233 }, // Tulcea
        { "RO-TM", 1681 }, // Timis
        { "RO-TR", 913 }, // Teleorman
        { "RO-VL", 1042 }, // Valcea
        { "RO-VN", 767 }, // Vrancea
        { "RO-VS", 1026 }, // Vaslui
    };

    private static readonly Dictionary<string, int> BG = new()
    {
        { "BG-01", 1545 }, // Blagoevgrad
        { "BG-02", 1861 }, // Burgas
        { "BG-03", 1055 }, // Varna
        { "BG-04", 1146 }, // Veliko Tarnovo
        { "BG-05", 708 }, // Vidin
        { "BG-06", 859 }, // Vratsa
        { "BG-07", 567 }, // Gabrovo
        { "BG-08", 1096 }, // Dobrich
        { "BG-09", 852 }, // Kardzhali
        { "BG-10", 755 }, // Kyustendil
        { "BG-11", 961 }, // Lovech
        { "BG-12", 856 }, // Montana
        { "BG-13", 1063 }, // Pazardzhik
        { "BG-14", 602 }, // Pernik
        { "BG-15", 1113 }, // Pleven
        { "BG-16", 1635 }, // Plovdiv
        { "BG-17", 568 }, // Razgrad
        { "BG-18", 743 }, // Ruse
        { "BG-19", 665 }, // Silistra
        { "BG-20", 864 }, // Sliven
        { "BG-21", 763 }, // Smolyan
        { "BG-22", 978 }, // Sofia (stolitsa)
        { "BG-23", 1796 }, // Sofia
        { "BG-24", 1292 }, // Stara Zagora
        { "BG-25", 660 }, // Targovishte
        { "BG-26", 1388 }, // Haskovo
        { "BG-27", 827 }, // Shumen
        { "BG-28", 781 }, // Yambol
    };

    private static readonly Dictionary<string, int> MK = new()
    {
        { "MK-101", 982 }, // Veles
        { "MK-102", 428 }, // Gradsko
        { "MK-103", 522 }, // Demir Kapija
        { "MK-104", 1815 }, // Kavadarci
        { "MK-105", 275 }, // Lozovo
        { "MK-106", 765 }, // Negotino
        { "MK-107", 230 }, // Rosoman
        { "MK-108", 822 }, // Sveti Nikole
        { "MK-201", 968 }, // Berovo
        { "MK-202", 783 }, // Vinica
        { "MK-203", 755 }, // Delčevo
        { "MK-205", 382 }, // Karbinci
        { "MK-206", 658 }, // Kocani
        { "MK-207", 341 }, // Makedonska Kamenica
        { "MK-208", 358 }, // Pehcevo
        { "MK-209", 551 }, // Probistip
        { "MK-210", 229 }, // Cesinovo-Oblesevo
        { "MK-211", 1076 }, // Stip
        { "MK-304", 716 }, // Debrca
        { "MK-307", 1498 }, // Kicevo
        { "MK-310", 961 }, // Ohrid
        { "MK-402", 37 }, // Bosilovo
        { "MK-403", 5098 }, // Valandovo
        { "MK-404", 41 }, // Vasilevo
        { "MK-405", 570 }, // Gevgelija
        { "MK-406", 241 }, // Dojran
        { "MK-408", 675 }, // Novo Selo
        { "MK-409", 830 }, // Radovis
        { "MK-410", 211 }, // Strumica
        { "MK-501", 1710 }, // Bitola
        { "MK-506", 417 }, // Mogila
        { "MK-508", 2200 }, // Prilep
        { "MK-509", 64 }, // Resen
        { "MK-601", 64 }, // Bogovinje
        { "MK-602", 281 }, // Brvenica
        { "MK-603", 362 }, // Vrapčište
        { "MK-604", 944 }, // Gostivar
        { "MK-605", 369 }, // Zelino
        { "MK-607", 1064 }, // Mavrovo i Rostusa
        { "MK-609", 666 }, // Tetovo
        { "MK-701", 673 }, // Kratovo
        { "MK-702", 803 }, // Kriva Palanka
        { "MK-703", 1370 }, // Kumanovo
        { "MK-704", 457 }, // Lipkovo
        { "MK-705", 388 }, // Rankovce
        { "MK-706", 697 }, // Staro Nagoričane
        { "MK-802", 95 }, // Aračinovo
        { "MK-807", 232 }, // Ilinden
        { "MK-810", 386 }, // Petrovec
        { "MK-816", 391 }, // Cucer Sandevo
        { "MK-85", 3818 }, // Skopje
    };

    private static readonly Dictionary<string, int> ME = new()
    {
        { "ME-01", 470 }, // Andrijevica
        { "ME-02", 1204 }, // Bar
        { "ME-03", 862 }, // Berane
        { "ME-04", 1506 }, // Bijelo Polje
        { "ME-05", 340 }, // Budva
        { "ME-06", 1448 }, // Cetinje
        { "ME-07", 745 }, // Danilovgrad
        { "ME-08", 570 }, // Herceg-Novi
        { "ME-09", 1338 }, // Kolasin
        { "ME-10", 737 }, // Kotor
        { "ME-11", 535 }, // Zabljak
        { "ME-12", 3337 }, // Niksic
        { "ME-13", 617 }, // Plav
        { "ME-14", 2135 }, // Pljevlja
        { "ME-15", 1268 }, // Pluzine
        { "ME-16", 3313 }, // Podgorica
        { "ME-17", 736 }, // Rozaje
        { "ME-18", 816 }, // Šavnik
        { "ME-19", 171 }, // Tivat
        { "ME-20", 541 }, // Ulcinj
        { "ME-21", 665 }, // Zabljak
        { "ME-22", 244 }, // Gusinje
        { "ME-23", 288 }, // Petnjica
        { "ME-24", 116 }, // Tuzi
    };

    private static readonly Dictionary<string, int> RS = new()
    {
        { "RS-00", 1573 }, // Beograd
        { "RS-01", 553 }, // Severnobacki okrug
        { "RS-02", 918 }, // Srednjebanatski okrug
        { "RS-03", 665 }, // Severnobanatski okrug
        { "RS-04", 1222 }, // Juznobanatski okrug
        { "RS-05", 702 }, // Zapadnobacki okrug
        { "RS-06", 1405 }, // Juznobacki okrug
        { "RS-07", 1123 }, // Sremski okrug
        { "RS-08", 958 }, // Macvanski okrug
        { "RS-09", 742 }, // Kolubarski okrug
        { "RS-10", 435 }, // Podunavski okrug
        { "RS-11", 1048 }, // Branicevski okrug
        { "RS-12", 751 }, // Sumadijski okrug
        { "RS-13", 802 }, // Pomoravski okrug
        { "RS-14", 969 }, // Borski okrug
        { "RS-15", 1031 }, // Zajecarski okrug
        { "RS-16", 1696 }, // Zlatiborski okrug
        { "RS-17", 903 }, // Moravicki okrug
        { "RS-18", 1153 }, // Raski okrug
        { "RS-19", 799 }, // Rasinski okrug
        { "RS-20", 960 }, // Nisavski okrug
        { "RS-21", 664 }, // Toplicki okrug
        { "RS-22", 762 }, // Pirotski okrug
        { "RS-23", 855 }, // Jablanicki okrug
        { "RS-24", 1013 }, // Pcinjski okrug
    };

    private static readonly Dictionary<string, int> SI = new()
    {
        { "SI-001", 2517 }, // Ajdovscina
        { "SI-002", 673 }, // Beltinci
        { "SI-003", 771 }, // Bled
        { "SI-004", 191 }, // Bohinj
        { "SI-005", 438 }, // Borovnica
        { "SI-006", 3251 }, // Bovec
        { "SI-007", 754 }, // Brda
        { "SI-008", 1004 }, // Brezovica
        { "SI-009", 2836 }, // Brezice
        { "SI-010", 395 }, // Tisina
        { "SI-011", 1595 }, // Celje
        { "SI-012", 894 }, // Cerklje na Gorenjskem
        { "SI-013", 2360 }, // Cerknica
        { "SI-014", 1301 }, // Cerkno
        { "SI-015", 348 }, // Crensovci
        { "SI-016", 1416 }, // Črna na Koroškem
        { "SI-017", 3241 }, // Crnomelj
        { "SI-018", 351 }, // Destrnik
        { "SI-019", 1434 }, // Divaca
        { "SI-020", 916 }, // Dobrepolje
        { "SI-021", 1160 }, // Dobrova-Polhov Gradec
        { "SI-022", 396 }, // Dol pri Ljubljani
        { "SI-023", 1143 }, // Domzale
        { "SI-024", 317 }, // Dornava
        { "SI-025", 1113 }, // Dravograd
        { "SI-026", 472 }, // Duplek
        { "SI-027", 1564 }, // Gorenja vas-Poljane
        { "SI-028", 610 }, // Gorišnica
        { "SI-029", 839 }, // Gornja Radgona
        { "SI-030", 857 }, // Gornji Grad
        { "SI-031", 700 }, // Gornji Petrovci
        { "SI-032", 1518 }, // Grosuplje
        { "SI-033", 697 }, // Salovci
        { "SI-034", 689 }, // Hrastnik
        { "SI-035", 1811 }, // Hrpelje-Kozina
        { "SI-036", 2745 }, // Idrija
        { "SI-037", 1051 }, // Ig
        { "SI-038", 4519 }, // Ilirska Bistrica
        { "SI-039", 2343 }, // Ivancna Gorica
        { "SI-040", 492 }, // Izola
        { "SI-041", 940 }, // Jesenice
        { "SI-042", 372 }, // Jursinci
        { "SI-043", 2828 }, // Kamnik
        { "SI-044", 1423 }, // Kanal
        { "SI-045", 716 }, // Kidricevo
        { "SI-046", 1760 }, // Kobarid
        { "SI-047", 179 }, // Kobilje
        { "SI-048", 5303 }, // Kocevje
        { "SI-049", 1022 }, // Komen
        { "SI-050", 3558 }, // Koper
        { "SI-051", 864 }, // Kozje
        { "SI-052", 872 }, // Kranj
        { "SI-053", 2328 }, // Kranjska Gora
        { "SI-054", 3156 }, // Krsko
        { "SI-055", 491 }, // Kungota
        { "SI-056", 240 }, // Kuzma
        { "SI-057", 2070 }, // Lasko
        { "SI-058", 710 }, // Lenart
        { "SI-059", 1267 }, // Lendava
        { "SI-060", 2292 }, // Litija
        { "SI-061", 6041 }, // Ljubljana
        { "SI-062", 719 }, // Ljubno
        { "SI-063", 1262 }, // Ljutomer
        { "SI-064", 1766 }, // Logatec
        { "SI-065", 1566 }, // Loska dolina
        { "SI-066", 1262 }, // Loski Potok
        { "SI-067", 1010 }, // Luce
        { "SI-068", 809 }, // Lukovica
        { "SI-069", 715 }, // Majsperk
        { "SI-070", 2767 }, // Maribor
        { "SI-071", 911 }, // Medvode
        { "SI-072", 301 }, // Menges
        { "SI-073", 1100 }, // Metlika
        { "SI-074", 276 }, // Mezica
        { "SI-075", 646 }, // Miren-Kostanjevica
        { "SI-076", 1098 }, // Mislinja
        { "SI-077", 654 }, // Moravce
        { "SI-078", 1463 }, // Moravske Toplice
        { "SI-079", 572 }, // Mozirje
        { "SI-080", 832 }, // Murska Sobota
        { "SI-081", 399 }, // Muta
        { "SI-082", 331 }, // Naklo
        { "SI-083", 441 }, // Nazarje
        { "SI-084", 2949 }, // Nova Gorica
        { "SI-085", 3196 }, // Novo Mesto
        { "SI-086", 78 }, // Odranci
        { "SI-087", 1520 }, // Ormoz
        { "SI-088", 332 }, // Osilnica
        { "SI-089", 858 }, // Pesnica
        { "SI-090", 673 }, // Piran
        { "SI-091", 2128 }, // Pivka
        { "SI-092", 614 }, // Podcetrtek
        { "SI-093", 945 }, // Podvelka
        { "SI-094", 2613 }, // Postojna
        { "SI-095", 839 }, // Preddvor
        { "SI-096", 538 }, // Ptuj
        { "SI-097", 1149 }, // Puconci
        { "SI-098", 554 }, // Race-Fram
        { "SI-099", 545 }, // Radece
        { "SI-100", 408 }, // Radenci
        { "SI-101", 897 }, // Radlje ob Dravi
        { "SI-102", 1366 }, // Radovljica
        { "SI-103", 747 }, // Ravne na Koroskem
        { "SI-104", 1518 }, // Ribnica
        { "SI-105", 455 }, // Rogasovci
        { "SI-106", 804 }, // Rogaska Slatina
        { "SI-107", 388 }, // Rogatec
        { "SI-108", 621 }, // Ruse
        { "SI-109", 1349 }, // Semic
        { "SI-110", 2696 }, // Sevnica
        { "SI-111", 2271 }, // Sezana
        { "SI-112", 1929 }, // Slovenj Gradec
        { "SI-113", 2815 }, // Slovenska Bistrica
        { "SI-114", 1214 }, // Slovenske Konjice
        { "SI-115", 368 }, // Starse
        { "SI-116", 76 }, // Sveti Jurij ob Scavnici
        { "SI-117", 510 }, // Sencur
        { "SI-118", 723 }, // Sentilj
        { "SI-119", 989 }, // Sentjernej
        { "SI-120", 2461 }, // Sentjur
        { "SI-121", 584 }, // Skocjan
        { "SI-122", 1701 }, // Skofja Loka
        { "SI-123", 582 }, // Skofljica
        { "SI-124", 1178 }, // Smarje pri Jelsah
        { "SI-125", 243 }, // Smartno ob Paki
        { "SI-126", 1098 }, // Sostanj
        { "SI-127", 351 }, // Store
        { "SI-128", 3584 }, // Tolmin
        { "SI-129", 771 }, // Trbovlje
        { "SI-130", 2069 }, // Trebnje
        { "SI-131", 1588 }, // Trzic
        { "SI-132", 258 }, // Turnisce
        { "SI-133", 1291 }, // Velenje
        { "SI-134", 1056 }, // Velike Lasce
        { "SI-135", 809 }, // Videm
        { "SI-136", 1065 }, // Vipava
        { "SI-137", 628 }, // Vitanje
        { "SI-138", 398 }, // Vodice
        { "SI-139", 927 }, // Vojnik
        { "SI-140", 1306 }, // Vrhnika
        { "SI-141", 490 }, // Vuzenica
        { "SI-142", 1644 }, // Zagorje ob Savi
        { "SI-143", 188 }, // Zavrc
        { "SI-144", 735 }, // Zrece
        { "SI-146", 1588 }, // Zelezniki
        { "SI-147", 523 }, // Ziri
        { "SI-148", 260 }, // Benedikt
        { "SI-149", 297 }, // Bistrica ob Sotli
        { "SI-150", 771 }, // Bloke
        { "SI-151", 606 }, // Braslovce
        { "SI-152", 316 }, // Cankova
        { "SI-153", 242 }, // Cerkvenjak
        { "SI-154", 171 }, // Dobje
        { "SI-155", 343 }, // Dobrna
        { "SI-156", 308 }, // Dobrovnik
        { "SI-157", 1032 }, // Dolenjske Toplice
        { "SI-158", 418 }, // Grad
        { "SI-159", 264 }, // Hajdina
        { "SI-160", 646 }, // Hoce-Slivnica
        { "SI-161", 176 }, // Hodos
        { "SI-162", 344 }, // Horjul
        { "SI-163", 608 }, // Jezersko
        { "SI-164", 318 }, // Komenda
        { "SI-165", 527 }, // Kostel
        { "SI-166", 487 }, // Krizevci
        { "SI-167", 773 }, // Lovrenc na Pohorju
        { "SI-168", 326 }, // Markovci
        { "SI-169", 193 }, // Miklavz na Dravskem polju
        { "SI-170", 517 }, // Mirna Pec
        { "SI-171", 389 }, // Oplotnica
        { "SI-172", 448 }, // Podlehnik
        { "SI-173", 436 }, // Polzela
        { "SI-174", 503 }, // Prebold
        { "SI-175", 600 }, // Prevalje
        { "SI-176", 105 }, // Razkrizje
        { "SI-177", 534 }, // Ribnica na Pohorju
        { "SI-178", 615 }, // Selnica ob Dravi
        { "SI-179", 516 }, // Sodrazica
        { "SI-180", 913 }, // Solcava
        { "SI-181", 375 }, // Sveta Ana
        { "SI-182", 183 }, // Sveti Andraz v Slovenskih Goricah
        { "SI-183", 227 }, // Sempeter-Vrtojba
        { "SI-184", 352 }, // Tabor
        { "SI-185", 215 }, // Trnovska Vas
        { "SI-186", 124 }, // Trzin
        { "SI-187", 179 }, // Velika Polana
        { "SI-188", 126 }, // Verzej
        { "SI-189", 544 }, // Vransko
        { "SI-190", 1518 }, // Zalec
        { "SI-191", 357 }, // Zetale
        { "SI-192", 426 }, // Žirovnica
        { "SI-193", 1517 }, // Zuzemberk
        { "SI-194", 967 }, // Smartno pri Litiji
        { "SI-195", 521 }, // Apace
        { "SI-196", 311 }, // Cirkulane
        { "SI-197", 573 }, // Kosanjevica na Krki
        { "SI-198", 374 }, // Makole
        { "SI-199", 721 }, // Mokronog-Trebelno
        { "SI-200", 416 }, // Poljcane
        { "SI-201", 353 }, // Rence-Vogrsko
        { "SI-202", 325 }, // Središče ob Dravi
        { "SI-203", 409 }, // Straza
        { "SI-204", 272 }, // Sveta Trojica v Slovenskih goricah
        { "SI-205", 415 }, // Sveti Tomaz
        { "SI-206", 494 }, // Smarjeske Toplice
        { "SI-207", 1068 }, // Gorje
        { "SI-208", 154 }, // Log-Dragomer
        { "SI-209", 311 }, // Recica ob Savinji
        { "SI-210", 359 }, // Sveti Jurij v Slovenskih goricah
        { "SI-211", 498 }, // Sentrupert
        { "SI-212", 340 }, // Mirna
        { "SI-213", 121 }, // Ankaran
    };

    private static readonly Dictionary<string, int> CH = new()
    {
        { "CH-AG", 1156 }, // Aargau
        { "CH-AI", 107 }, // Appenzell Innerrhoden
        { "CH-AR", 170 }, // Appenzell Ausserrhoden
        { "CH-BE", 3736 }, // Bern
        { "CH-BL", 435 }, // Basel-Landschaft
        { "CH-BS", 91 }, // Basel-Stadt
        { "CH-FR", 999 }, // Fribourg
        { "CH-GE", 320 }, // Geneve
        { "CH-GL", 383 }, // Glarus
        { "CH-GR", 3844 }, // Graubunden
        { "CH-JU", 476 }, // Jura
        { "CH-LU", 986 }, // Luzern
        { "CH-NE", 493 }, // Neuchatel
        { "CH-NW", 151 }, // Nidwalden
        { "CH-OW", 283 }, // Obwalden
        { "CH-SG", 1346 }, // Sankt Gallen
        { "CH-SH", 201 }, // Schaffhausen
        { "CH-SO", 658 }, // Solothurn
        { "CH-SZ", 537 }, // Schwyz
        { "CH-TG", 683 }, // Thurgau
        { "CH-TI", 1551 }, // Ticino
        { "CH-UR", 576 }, // Uri
        { "CH-VD", 2045 }, // Vaud
        { "CH-VS", 2969 }, // Valais
        { "CH-ZG", 172 }, // Zug
        { "CH-ZH", 1631 }, // Zurich
    };

    private static readonly Dictionary<string, int> AD = new()
    {
        { "AD-02", 1650 }, // Canillo
        { "AD-03", 1123 }, // Encamp
        { "AD-04", 1057 }, // La Massana
        { "AD-05", 1186 }, // Ordino
        { "AD-06", 1003 }, // Sant Julia de Loria
        { "AD-07", 418 }, // Andorra la Vella
        { "AD-08", 562 }, // Escaldes-Engordany
    };

    private static readonly Dictionary<string, int> SM = new()
    {
        { "SM-01", 631 }, // Acquaviva
        { "SM-02", 727 }, // Chiesanuova
        { "SM-03", 955 }, // Domagnano
        { "SM-04", 1005 }, // Faetano
        { "SM-05", 884 }, // Fiorentino
        { "SM-06", 1497 }, // Borgo Maggiore
        { "SM-07", 1096 }, // Citta di San Marino
        { "SM-08", 430 }, // Montegiardino
        { "SM-09", 1775 }, // Serravalle
    };

    private static readonly Dictionary<string, int> MC = new()
    {
        { "MC-1", 100 }, // Monaco
    };

    private static readonly Dictionary<string, int> GI = new()
    {
        { "Gibraltar", 100 }, // Gibraltar
    };

    private static readonly Dictionary<string, int> IE = new()
    {
        { "IE-CE", 1739 }, // Clare
        { "IE-CN", 354 }, // Cavan
        { "IE-CO", 2886 }, // Cork
        { "IE-CW", 293 }, // Carlow
        { "IE-D", 2110 }, // Dublin
        { "IE-DL", 739 }, // Donegal
        { "IE-G", 1408 }, // Galway
        { "IE-KE", 1524 }, // Kildare
        { "IE-KK", 1344 }, // Kilkenny
        { "IE-KY", 2730 }, // Kerry
        { "IE-LD", 188 }, // Longford
        { "IE-LH", 725 }, // Louth
        { "IE-LK", 608 }, // Limerick
        { "IE-LM", 84 }, // Leitrim
        { "IE-LS", 310 }, // Laois
        { "IE-MH", 1052 }, // Meath
        { "IE-MN", 756 }, // Monaghan
        { "IE-MO", 2673 }, // Mayo
        { "IE-OY", 416 }, // Offaly
        { "IE-RN", 289 }, // Roscommon
        { "IE-SO", 308 }, // Sligo
        { "IE-TA", 742 }, // Tipperary
        { "IE-WD", 705 }, // Waterford
        { "IE-WH", 418 }, // Westmeath
        { "IE-WW", 1065 }, // Wicklow
        { "IE-WX", 534 }, // Wexford
    };

    private static readonly Dictionary<string, int> GB = new()
    {
        { "GB-ENG", 1500 }, // England
        { "GB-NIR", 750 }, // Northern Ireland
        { "GB-SCT", 1000 }, // Scotland
        { "GB-WLS", 600 }, // Wales
    };

    private static readonly Dictionary<string, int> TR = new()
    {
        { "TR-01", 1811 }, // Adana
        { "TR-02", 678 }, // Adiyaman
        { "TR-03", 1884 }, // Afyonkarahisar
        { "TR-04", 549 }, // Agri
        { "TR-05", 555 }, // Amasya
        { "TR-06", 5755 }, // Ankara
        { "TR-07", 2664 }, // Antalya
        { "TR-08", 317 }, // Artvin
        { "TR-09", 809 }, // Aydin
        { "TR-10", 3543 }, // Balikesir
        { "TR-11", 823 }, // Bilecik
        { "TR-12", 283 }, // Bingol
        { "TR-13", 436 }, // Bitlis
        { "TR-14", 1460 }, // Bolu
        { "TR-15", 578 }, // Burdur
        { "TR-16", 3606 }, // Bursa
        { "TR-17", 2039 }, // Canakkale
        { "TR-18", 941 }, // Cankiri
        { "TR-19", 1062 }, // Corum
        { "TR-20", 886 }, // Denizli
        { "TR-21", 797 }, // Diyarbakir
        { "TR-22", 1595 }, // Edirne
        { "TR-23", 588 }, // Elazig
        { "TR-24", 546 }, // Erzincan
        { "TR-25", 1202 }, // Erzurum
        { "TR-26", 2244 }, // Eskisehir
        { "TR-27", 1381 }, // Gaziantep
        { "TR-28", 605 }, // Giresun
        { "TR-29", 405 }, // Gumushane
        { "TR-30", 166 }, // Hakkari
        { "TR-31", 1048 }, // Hatay
        { "TR-32", 1068 }, // Isparta
        { "TR-33", 1862 }, // Mersin
        { "TR-34", 4062 }, // Istanbul
        { "TR-35", 3266 }, // Izmir
        { "TR-36", 539 }, // Kars
        { "TR-37", 1282 }, // Kastamonu
        { "TR-38", 1966 }, // Kayseri
        { "TR-39", 1448 }, // Kirklareli
        { "TR-40", 941 }, // Kirsehir
        { "TR-41", 2106 }, // Kocaeli
        { "TR-42", 6044 }, // Konya
        { "TR-43", 1903 }, // Kutahya
        { "TR-44", 903 }, // Malatya
        { "TR-45", 1711 }, // Manisa
        { "TR-46", 1046 }, // Kahramanmaras
        { "TR-47", 622 }, // Mardin
        { "TR-48", 1481 }, // Mugla
        { "TR-49", 362 }, // Mus
        { "TR-50", 1028 }, // Nevsehir
        { "TR-51", 921 }, // Nigde
        { "TR-52", 1378 }, // Ordu
        { "TR-53", 493 }, // Rize
        { "TR-54", 2230 }, // Sakarya
        { "TR-55", 2409 }, // Samsun
        { "TR-56", 341 }, // Siirt
        { "TR-57", 732 }, // Sinop
        { "TR-58", 1341 }, // Sivas
        { "TR-59", 2187 }, // Tekirdag
        { "TR-60", 887 }, // Tokat
        { "TR-61", 1130 }, // Trabzon
        { "TR-62", 155 }, // Tunceli
        { "TR-63", 1718 }, // Sanliurfa
        { "TR-64", 397 }, // Usak
        { "TR-65", 950 }, // Van
        { "TR-66", 789 }, // Yozgat
        { "TR-67", 1179 }, // Zonguldak
        { "TR-68", 1517 }, // Aksaray
        { "TR-69", 218 }, // Bayburt
        { "TR-70", 962 }, // Karaman
        { "TR-71", 761 }, // Kirikkale
        { "TR-72", 480 }, // Batman
        { "TR-73", 374 }, // Sirnak
        { "TR-74", 514 }, // Bartin
        { "TR-75", 225 }, // Ardahan
        { "TR-76", 265 }, // Igdir
        { "TR-77", 484 }, // Yalova
        { "TR-78", 549 }, // Karabuk
        { "TR-79", 192 }, // Kilis
        { "TR-80", 396 }, // Osmaniye
        { "TR-81", 945 }, // Duzce
    };

    private static readonly Dictionary<string, int> UA = new()
    {
        { "UA-05", 1019 }, // Vinnytska oblast
        { "UA-07", 752 }, // Volynska oblast
        { "UA-12", 1461 }, // Dnipropetrovska oblast
        { "UA-14", 1119 }, // Donetska oblast
        { "UA-18", 1092 }, // Zhytomyrska oblast
        { "UA-21", 584 }, // Zakarpatska oblast
        { "UA-23", 1005 }, // Zaporizka oblast
        { "UA-26", 619 }, // Ivano-Frankivska oblast
        { "UA-30", 198 }, // Kyiv
        { "UA-32", 1222 }, // Kyivska oblast
        { "UA-35", 932 }, // Kirovohradska oblast
        { "UA-46", 1075 }, // Lvivska oblast
        { "UA-48", 938 }, // Mykolaivska oblast
        { "UA-51", 1279 }, // Odeska oblast
        { "UA-53", 1096 }, // Poltavska oblast
        { "UA-56", 767 }, // Rivnenska oblast
        { "UA-59", 931 }, // Sumska oblast
        { "UA-61", 563 }, // Ternopilska oblast
        { "UA-63", 1292 }, // Kharkivska oblast
        { "UA-65", 971 }, // Khersonska oblast
        { "UA-68", 792 }, // Khmelnytska oblast
        { "UA-71", 845 }, // Cherkaska oblast
        { "UA-74", 1136 }, // Chernihivska oblast
        { "UA-77", 348 }, // Chernivetska oblast
    };

    private static readonly Dictionary<string, int> AL = new()
    {
        { "AL-01", 672 }, // Berat
        { "AL-02", 468 }, // Durres
        { "AL-03", 1284 }, // Elbasan
        { "AL-04", 829 }, // Fier
        { "AL-05", 1054 }, // Gjirokaster
        { "AL-06", 1435 }, // Korce
        { "AL-07", 880 }, // Kukes
        { "AL-08", 661 }, // Lezhe
        { "AL-09", 960 }, // Diber
        { "AL-10", 1446 }, // Shkoder
        { "AL-11", 1189 }, // Tirane
        { "AL-12", 1120 }, // Vlore
    };

    private static readonly Dictionary<string, int> FO = new()
    {
        { "Norðoyar region", 300 }, // Norðoyar region
        { "Streymoy region", 600 }, // Streymoy region
        { "Vágar region", 300 }, // Vágar region
        { "Eysturoy region", 650 }, // Vágar region
        { "Sandoy region", 250 }, // Vágar region
        { "Suðuroy region", 550 }, // Vágar region
    };

    private static readonly Dictionary<string, int> MT = new()
    {
        { "MT-01", 1519 }, // Attard
        { "MT-02", 83 }, // Balzan
        { "MT-03", 58 }, // Birgu
        { "MT-04", 843 }, // Birkirkara
        { "MT-05", 2118 }, // Birzebbuga
        { "MT-06", 118 }, // Bormla
        { "MT-07", 1134 }, // Dingli
        { "MT-08", 425 }, // Fgura
        { "MT-09", 65 }, // Floriana
        { "MT-10", 31 }, // Fontana
        { "MT-11", 519 }, // Gudja
        { "MT-12", 388 }, // Gzira
        { "MT-13", 1564 }, // Ghajnsielem
        { "MT-14", 886 }, // Gharb
        { "MT-15", 486 }, // Gharghur
        { "MT-16", 1099 }, // Ghasri
        { "MT-17", 783 }, // Ghaxaq
        { "MT-18", 383 }, // Hamrun
        { "MT-19", 295 }, // Iklin
        { "MT-20", 50 }, // Isla
        { "MT-21", 283 }, // Kalkara
        { "MT-22", 1121 }, // Kerċem
        { "MT-23", 281 }, // Kirkop
        { "MT-24", 274 }, // Lija
        { "MT-25", 1420 }, // Luqa
        { "MT-26", 579 }, // Marsa
        { "MT-27", 1298 }, // Marsaskala
        { "MT-28", 906 }, // Marsaxlokk
        { "MT-29", 15 }, // Mdina
        { "MT-30", 4939 }, // Mellieha
        { "MT-31", 3455 }, // Mgarr
        { "MT-32", 1749 }, // Mosta
        { "MT-33", 506 }, // Mqabba
        { "MT-34", 442 }, // Msida
        { "MT-35", 63 }, // Mtarfa
        { "MT-36", 460 }, // Munxar
        { "MT-37", 1612 }, // Nadur
        { "MT-38", 2621 }, // Naxxar
        { "MT-39", 631 }, // Paola
        { "MT-40", 497 }, // Pembroke
        { "MT-41", 90 }, // Pieta
        { "MT-42", 1119 }, // Qala
        { "MT-43", 1423 }, // Qormi
        { "MT-44", 928 }, // Qrendi
        { "MT-45", 615 }, // Rabat Gozo
        { "MT-46", 5665 }, // Rabat Malta
        { "MT-47", 494 }, // Safi
        { "MT-48", 425 }, // Saint Julian's
        { "MT-49", 682 }, // Saint John
        { "MT-50", 654 }, // Saint Lawrence
        { "MT-51", 3571 }, // Saint Paul's Bay
        { "MT-52", 688 }, // Sannat
        { "MT-53", 82 }, // Saint Lucia's
        { "MT-54", 155 }, // Santa Venera
        { "MT-55", 4257 }, // Siggiewi
        { "MT-56", 565 }, // Sliema
        { "MT-57", 877 }, // Swieqi
        { "MT-58", 47 }, // Ta' Xbiex
        { "MT-59", 177 }, // Tarxien
        { "MT-60", 117 }, // Valletta
        { "MT-61", 1666 }, // Xaghra
        { "MT-62", 985 }, // Xewkija
        { "MT-63", 247 }, // Xghajra
        { "MT-64", 1345 }, // Zabbar
        { "MT-65", 533 }, // Zebbug Gozo
        { "MT-66", 533 }, // Żebbuġ Malta
        { "MT-67", 1714 }, // Zejtun
        { "MT-68", 2348 }, // Zurrieq
    };

    private static readonly Dictionary<string, int> RU = new()
    {
        { "RU-AD", 586 }, // Adygeya, Respublika
        { "RU-AL", 1245 }, // Altay, Respublika
        { "RU-ALT", 1611 }, // Altayskiy kray
        { "RU-AMU", 1684 }, // Amurskaya oblast'
        { "RU-ARK", 1831 }, // Arkhangel'skaya oblast'
        { "RU-AST", 879 }, // Astrakhanskaya oblast'
        { "RU-BA", 1538 }, // Bashkortostan, Respublika
        { "RU-BEL", 879 }, // Belgorodskaya oblast'
        { "RU-BRY", 879 }, // Bryanskaya oblast'
        { "RU-BU", 1904 }, // Buryatiya, Respublika
        { "RU-CE", 662 }, // Chechenskaya Respublika
        { "RU-CHE", 1904 }, // Chelyabinskaya oblast'
        { "RU-CU", 710 }, // Chuvashskaya Respublika
        { "RU-DA", 1098 }, // Dagestan, Respublika
        { "RU-IN", 198 }, // Ingushetiya, Respublika
        { "RU-IRK", 1831 }, // Irkutskaya oblast'
        { "RU-IVA", 732 }, // Ivanovskaya oblast'
        { "RU-KAM", 1465 }, // Kamchatskiy kray
        { "RU-KB", 732 }, // Kabardino-Balkarskaya Respublika
        { "RU-KC", 622 }, // Karachayevo-Cherkesskaya Respublika
        { "RU-KDA", 1904 }, // Krasnodarskiy kray
        { "RU-KEM", 1684 }, // Kemerovskaya oblast'
        { "RU-KGD", 1611 }, // Kaliningradskaya oblast'
        { "RU-KGN", 1245 }, // Kurganskaya oblast'
        { "RU-KHA", 1391 }, // Khabarovskiy kray
        { "RU-KHM", 1977 }, // Khanty-Mansiyskiy avtonomnyy okrug
        { "RU-KIR", 1501 }, // Kirovskaya oblast'
        { "RU-KK", 1538 }, // Khakasiya, Respublika
        { "RU-KL", 915 }, // Kalmykiya, Respublika
        { "RU-KLU", 732 }, // Kaluzhskaya oblast'
        { "RU-KO", 1904 }, // Komi, Respublika
        { "RU-KOS", 806 }, // Kostromskaya oblast'
        { "RU-KR", 1684 }, // Kareliya, Respublika
        { "RU-KRS", 732 }, // Kurskaya oblast'
        { "RU-KYA", 1684 }, // Krasnoyarskiy kray
        { "RU-LEN", 1831 }, // Leningradskaya oblast'
        { "RU-LIP", 732 }, // Lipetskaya oblast'
        { "RU-MAG", 1538 }, // Magadanskaya oblast'
        { "RU-ME", 710 }, // Mariy El, Respublika
        { "RU-MO", 718 }, // Mordoviya, Respublika
        { "RU-MOS", 1611 }, // Moskovskaya oblast'
        { "RU-MOW", 220 }, // Moskva
        { "RU-MUR", 1757 }, // Murmanskaya oblast'
        { "RU-NEN", 29 }, // Nenetskiy avtonomnyy okrug
        { "RU-NGR", 1098 }, // Novgorodskaya oblast'
        { "RU-NIZ", 1391 }, // Nizhegorodskaya oblast'
        { "RU-NVS", 1538 }, // Novosibirskaya oblast'
        { "RU-OMS", 1684 }, // Omskaya oblast'
        { "RU-ORE", 1538 }, // Orenburgskaya oblast'
        { "RU-ORL", 696 }, // Orlovskaya oblast'
        { "RU-PER", 1538 }, // Permskiy kray
        { "RU-PNZ", 806 }, // Penzenskaya oblast'
        { "RU-PRI", 1757 }, // Primorskiy kray
        { "RU-PSK", 1098 }, // Pskovskaya oblast'
        { "RU-ROS", 1465 }, // Rostovskaya oblast'
        { "RU-RYA", 806 }, // Ryazanskaya oblast'
        { "RU-SA", 2197 }, // Saha, Respublika
        { "RU-SAK", 1684 }, // Sakhalinskaya oblast'
        { "RU-SAM", 1175 }, // Samarskaya oblast'
        { "RU-SAR", 1391 }, // Saratovskaya oblast'
        { "RU-SE", 659 }, // Severnaya Osetiya, Respublika
        { "RU-SMO", 842 }, // Smolenskaya oblast'
        { "RU-SPE", 110 }, // Sankt-Peterburg
        { "RU-STA", 1245 }, // Stavropol'skiy kray
        { "RU-SVE", 1684 }, // Sverdlovskaya oblast'
        { "RU-TA", 1391 }, // Tatarstan, Respublika
        { "RU-TAM", 732 }, // Tambovskaya oblast'
        { "RU-TOM", 1684 }, // Tomskaya oblast'
        { "RU-TUL", 879 }, // Tul'skaya oblast'
        { "RU-TVE", 1318 }, // Tverskaya oblast'
        { "RU-TY", 1245 }, // Tyva, Respublika
        { "RU-TYU", 1465 }, // Tyumenskaya oblast'
        { "RU-UD", 879 }, // Udmurtskaya Respublika
        { "RU-ULY", 879 }, // Ul'yanovskaya oblast'
        { "RU-VGG", 1465 }, // Volgogradskaya oblast'
        { "RU-VLA", 806 }, // Vladimirskaya oblast'
        { "RU-VLG", 1025 }, // Vologodskaya oblast'
        { "RU-VOR", 952 }, // Voronezhskaya oblast'
        { "RU-YAN", 1684 }, // Yamalo-Nenetskiy avtonomnyy okrug
        { "RU-YAR", 879 }, // Yaroslavskaya oblast'
        { "RU-YEV", 1025 }, // Yevreyskaya avtonomnaya oblast'
        { "RU-ZAB", 1684 }, // Zabaykal'skiy kray
    };

    private static readonly Dictionary<string, int> NG = new()
    {
        { "NG-AB", 250 }, // Abia
        { "NG-AD", 650 }, // Adamawa
        { "NG-AK", 477 }, // Akwa Ibom
        { "NG-AN", 354 }, // Anambra
        { "NG-BA", 1100 }, // Bauchi
        { "NG-BE", 1198 }, // Benue
        { "NG-BO", 1 }, // Borno
        { "NG-BY", 70 }, // Bayelsa
        { "NG-CR", 823 }, // Cross River
        { "NG-DE", 761 }, // Delta
        { "NG-EB", 555 }, // Ebonyi
        { "NG-ED", 887 }, // Edo
        { "NG-EK", 300 }, // Ekiti
        { "NG-EN", 702 }, // Enugu
        { "NG-FC", 895 }, // Abuja Federal Capital Territory
        { "NG-GO", 198 }, // Gombe
        { "NG-IM", 240 }, // Imo
        { "NG-JI", 410 }, // Jigawa
        { "NG-KD", 1959 }, // Kaduna
        { "NG-KE", 180 }, // Kebbi
        { "NG-KN", 1550 }, // Kano
        { "NG-KO", 490 }, // Kogi
        { "NG-KT", 130 }, // Katsina
        { "NG-KW", 840 }, // Kwara
        { "NG-LA", 965 }, // Lagos
        { "NG-NA", 670 }, // Nasarawa
        { "NG-NI", 1540 }, // Niger
        { "NG-OG", 942 }, // Ogun
        { "NG-ON", 320 }, // Ondo
        { "NG-OS", 940 }, // Osun
        { "NG-OY", 1100 }, // Oyo
        { "NG-PL", 740 }, // Plateau
        { "NG-RI", 540 }, // Rivers
        { "NG-SO", 262 }, // Sokoto
        { "NG-TA", 323 }, // Taraba
        { "NG-YO", 271 }, // Yobe
        { "NG-ZA", 170 }, // Zamfara
    };

    private static readonly Dictionary<string, int> SN = new()
    {
        { "SN-DB", 910 }, // Diourbel
        { "SN-DK", 970 }, // Dakar
        { "SN-FK", 165 }, // Fatick
        { "SN-KA", 115 }, // Kaffrine
        { "SN-KD", 256 }, // Kolda
        { "SN-KE", 124 }, // Kedougou
        { "SN-KL", 145 }, // Kaolack
        { "SN-LG", 390 }, // Louga
        { "SN-MT", 177 }, // Matam
        { "SN-SE", 220 }, // Sedhiou
        { "SN-SL", 480 }, // Saint-Louis
        { "SN-TC", 240 }, // Tambacounda
        { "SN-TH", 700 }, // Thies
        { "SN-ZG", 258 }, // Ziguinchor
    };

    private static readonly Dictionary<string, int> TN = new()
    {
        { "TN-11", 290 }, // Tunis
        { "TN-12", 77 }, // L'Ariana
        { "TN-13", 99 }, // Ben Arous
        { "TN-14", 22 }, // La Manouba
        { "TN-21", 150 }, // Nabeul
        { "TN-23", 111 }, // Bizerte
        { "TN-41", 55 }, // Kairouan
        { "TN-51", 215 }, // Sousse
        { "TN-52", 120 }, // Monastir
        { "TN-53", 150 }, // Mahdia
        { "TN-61", 350 }, // Sfax
        { "TN-81", 97 }, // Gabes
        { "TN-82", 210 }, // Medenine
    };

    private static readonly Dictionary<string, int> KE = new()
    {
        { "KE-01", 502 }, // Baringo
        { "KE-02", 234 }, // Bomet
        { "KE-03", 722 }, // Bungoma
        { "KE-04", 346 }, // Busia
        { "KE-05", 466 }, // Elgeyo/Marakwet
        { "KE-06", 633 }, // Embu
        { "KE-07", 494 }, // Garissa
        { "KE-08", 641 }, // Homa Bay
        { "KE-09", 254 }, // Isiolo
        { "KE-10", 927 }, // Kajiado
        { "KE-11", 793 }, // Kakamega
        { "KE-12", 436 }, // Kericho
        { "KE-13", 1376 }, // Kiambu
        { "KE-14", 549 }, // Kilifi
        { "KE-15", 448 }, // Kirinyaga
        { "KE-16", 1030 }, // Kisii
        { "KE-17", 485 }, // Kisumu
        { "KE-18", 859 }, // Kitui
        { "KE-19", 542 }, // Kwale
        { "KE-20", 1124 }, // Laikipia
        { "KE-22", 1108 }, // Machakos
        { "KE-23", 632 }, // Makueni
        { "KE-25", 424 }, // Marsabit
        { "KE-26", 1852 }, // Meru
        { "KE-27", 511 }, // Migori
        { "KE-28", 316 }, // Mombasa
        { "KE-29", 1108 }, // Murang'a
        { "KE-30", 1100 }, // Nairobi City
        { "KE-31", 1529 }, // Nakuru
        { "KE-32", 434 }, // Nandi
        { "KE-33", 811 }, // Narok
        { "KE-34", 605 }, // Nyamira
        { "KE-35", 314 }, // Nyandarua
        { "KE-36", 952 }, // Nyeri
        { "KE-37", 308 }, // Samburu
        { "KE-38", 647 }, // Siaya
        { "KE-39", 388 }, // Taita/Taveta
        { "KE-40", 265 }, // Tana River
        { "KE-41", 388 }, // Tharaka-Nithi
        { "KE-42", 594 }, // Trans Nzoia
        { "KE-43", 827 }, // Turkana
        { "KE-44", 1082 }, // Uasin Gishu
        { "KE-45", 241 }, // Vihiga
        { "KE-46", 459 }, // Wajir
        { "KE-47", 365 }, // West Pokot
    };

    private static readonly Dictionary<string, int> ZA = new()
    {
        { "ZA-EC", 6000 }, // Eastern Cape
        { "ZA-FS", 5000 }, // Free State
        { "ZA-GP", 3500 }, // Gauteng
        { "ZA-KZN", 5750 }, // Kwazulu-Natal
        { "ZA-LP", 5750 }, // Limpopo
        { "ZA-MP", 5000 }, // Mpumalanga
        { "ZA-NC", 7500 }, // Northern Cape
        { "ZA-NW", 5000 }, // North-West
        { "ZA-WC", 6750 }, // Western Cape
    };

    private static readonly Dictionary<string, int> BD = new()
    {
        { "BD-A", 2601 }, // Barishal
        { "BD-B", 7641 }, // Chattogram
        { "BD-C", 9188 }, // Dhaka
        { "BD-D", 8944 }, // Khulna
        { "BD-E", 5112 }, // Rajshahi
        { "BD-F", 2903 }, // Rangpur
        { "BD-G", 3573 }, // Sylhet
        { "BD-H", 4073 }, // Mymensingh
    };

    private static readonly Dictionary<string, int> MY = new()
    {
        { "MY-01", 7650 }, // Johor
        { "MY-02", 5100 }, // Kedah
        { "MY-03", 6630 }, // Kelantan
        { "MY-04", 2550 }, // Melaka
        { "MY-05", 4080 }, // Negeri Sembilan
        { "MY-06", 9690 }, // Pahang
        { "MY-07", 2550 }, // Pulau Pinang
        { "MY-08", 7650 }, // Perak
        { "MY-09", 2550 }, // Perlis
        { "MY-10", 5100 }, // Selangor
        { "MY-11", 6630 }, // Terengganu
        { "MY-12", 18360 }, // Sabah
        { "MY-13", 21420 }, // Sarawak
        { "MY-14", 550 }, // Wilayah Persekutuan Kuala Lumpur
        { "MY-15", 155 }, // Wilayah Persekutuan Labuan
        { "MY-16", 120 }, // Wilayah Persekutuan Putrajaya
    };

    private static readonly Dictionary<string, int> TH = new()
    {
        { "TH-10", 2325 }, // Krung Thep Maha Nakhon
        { "TH-11", 1156 }, // Samut Prakan
        { "TH-12", 992 }, // Nonthaburi
        { "TH-13", 1961 }, // Pathum Thani
        { "TH-14", 2832 }, // Phra Nakhon Si Ayutthaya
        { "TH-15", 1024 }, // Ang Thong
        { "TH-16", 3477 }, // Lop Buri
        { "TH-17", 860 }, // Sing Buri
        { "TH-18", 2137 }, // Chai Nat
        { "TH-19", 2746 }, // Saraburi
        { "TH-20", 4337 }, // Chon Buri
        { "TH-21", 3403 }, // Rayong
        { "TH-22", 3256 }, // Chanthaburi
        { "TH-23", 1632 }, // Trat
        { "TH-24", 4129 }, // Chachoengsao
        { "TH-25", 2652 }, // Prachin Buri
        { "TH-26", 1459 }, // Nakhon Nayok
        { "TH-27", 3527 }, // Sa Kaeo
        { "TH-30", 12073 }, // Nakhon Ratchasima
        { "TH-31", 6887 }, // Buri Ram
        { "TH-32", 6124 }, // Surin
        { "TH-33", 5207 }, // Si Sa Ket
        { "TH-34", 9253 }, // Ubon Ratchathani
        { "TH-35", 3039 }, // Yasothon
        { "TH-36", 6636 }, // Chaiyaphum
        { "TH-37", 2511 }, // Amnat Charoen
        { "TH-38", 2443 }, // Bueng Kan
        { "TH-39", 2514 }, // Nong Bua Lam Phu
        { "TH-40", 6962 }, // Khon Kaen
        { "TH-41", 7913 }, // Udon Thani
        { "TH-42", 3557 }, // Loei
        { "TH-43", 2590 }, // Nong Khai
        { "TH-44", 3600 }, // Maha Sarakham
        { "TH-45", 4734 }, // Roi Et
        { "TH-46", 3909 }, // Kalasin
        { "TH-47", 5704 }, // Sakon Nakhon
        { "TH-48", 3008 }, // Nakhon Phanom
        { "TH-49", 1754 }, // Mukdahan
        { "TH-50", 5924 }, // Chiang Mai
        { "TH-51", 1890 }, // Lamphun
        { "TH-52", 3774 }, // Lampang
        { "TH-53", 2297 }, // Uttaradit
        { "TH-54", 1925 }, // Phrae
        { "TH-55", 2598 }, // Nan
        { "TH-56", 2226 }, // Phayao
        { "TH-57", 4485 }, // Chiang Rai
        { "TH-58", 1651 }, // Mae Hong Son
        { "TH-60", 6040 }, // Nakhon Sawan
        { "TH-61", 2388 }, // Uthai Thani
        { "TH-62", 3979 }, // Kamphaeng Phet
        { "TH-63", 2555 }, // Tak
        { "TH-64", 3629 }, // Sukhothai
        { "TH-65", 4845 }, // Phitsanulok
        { "TH-66", 3078 }, // Phichit
        { "TH-67", 4253 }, // Phetchabun
        { "TH-70", 3867 }, // Ratchaburi
        { "TH-71", 5154 }, // Kanchanaburi
        { "TH-72", 4843 }, // Suphan Buri
        { "TH-73", 2816 }, // Nakhon Pathom
        { "TH-74", 1144 }, // Samut Sakhon
        { "TH-75", 496 }, // Samut Songkhram
        { "TH-76", 2461 }, // Phetchaburi
        { "TH-77", 2995 }, // Prachuap Khiri Khan
        { "TH-80", 6473 }, // Nakhon Si Thammarat
        { "TH-81", 2293 }, // Krabi
        { "TH-82", 1402 }, // Phangnga
        { "TH-83", 601 }, // Phuket
        { "TH-84", 8012 }, // Surat Thani
        { "TH-85", 878 }, // Ranong
        { "TH-86", 4026 }, // Chumphon
        { "TH-90", 4417 }, // Songkhla
        { "TH-91", 1307 }, // Satun
        { "TH-92", 3591 }, // Trang
        { "TH-93", 2468 }, // Phatthalung
        { "TH-94", 1677 }, // Pattani
        { "TH-95", 1333 }, // Yala
        { "TH-96", 1630 }, // Narathiwat
    };

    private static readonly Dictionary<string, int> LA = new()
    {
        { "LA-CH", 20 }, // Champasak
        { "LA-LP", 12 }, // Louangphabang
        { "LA-SV", 26 }, // Savannakhet
        { "LA-VI", 4 }, // Viangchan
        { "LA-VT", 210 }, // Viangchan
        { "LA-XA", 0 }, // Xaignabouli
    };

    private static readonly Dictionary<string, int> PH = new()
    {
        { "PH-00", 982 }, // National Capital Region
        { "PH-01", 7235 }, //
        { "PH-02", 5706 }, //
        { "PH-03", 11055 }, //
        { "PH-05", 7176 }, //
        { "PH-06", 10265 }, //
        { "PH-07", 10257 }, //
        { "PH-08", 5669 }, //
        { "PH-09", 2949 }, //
        { "PH-10", 5579 }, //
        { "PH-11", 5896 }, //
        { "PH-12", 5080 }, //
        { "PH-13", 2071 }, //
        { "PH-14", 662 }, //
        { "PH-15", 2748 }, //
        { "PH-40", 8851 }, //
        { "PH-41", 6173 }, //
    };

    private static readonly Dictionary<string, int> LK = new()
    {
        { "LK-1", 4292 }, // Western Province
        { "LK-2", 3291 }, // Central Province
        { "LK-3", 4075 }, // Southern Province
        { "LK-4", 3422 }, // Northern Province
        { "LK-5", 2983 }, // Eastern Province
        { "LK-6", 4753 }, // North Western Province
        { "LK-7", 5451 }, // North Central Province
        { "LK-8", 2816 }, // Uva Province
        { "LK-9", 2611 }, // Sabaragamuwa Province
    };

    private static readonly Dictionary<string, int> IN = new()
    {
        { "IN-AP", 50641 }, // Andhra Pradesh
        { "IN-AR", 2065 }, // Arunachal Pradesh
        { "IN-AS", 25967 }, // Assam
        { "IN-BR", 64068 }, // Bihar
        { "IN-CH", 179 }, // Chandigarh
        { "IN-CT", 19668 }, // Chhattisgarh
        { "IN-DL", 2092 }, // Delhi
        { "IN-GA", 2778 }, // Goa
        { "IN-GJ", 81463 }, // Gujarat
        { "IN-HP", 1195 }, // Himachal Pradesh
        { "IN-HR", 25525 }, // Haryana
        { "IN-JH", 24536 }, // Jharkhand
        { "IN-KA", 89289 }, // Karnataka
        { "IN-KL", 38911 }, // Kerala
        { "IN-MH", 139085 }, // Maharashtra
        { "IN-ML", 2783 }, // Meghalaya
        { "IN-MP", 91836 }, // Madhya Pradesh
        { "IN-MZ", 1118 }, // Mizoram
        { "IN-OR", 26320 }, // Odisha
        { "IN-PB", 25856 }, // Punjab
        { "IN-PY", 564 }, // Puducherry
        { "IN-RJ", 120519 }, // Rajasthan
        { "IN-SK", 828 }, // Sikkim
        { "IN-TG", 32735 }, // Telangana
        { "IN-TN", 80007 }, // Tamil Nadu
        { "IN-TR", 2704 }, // Tripura
        { "IN-UP", 99969 }, // Uttar Pradesh
        { "IN-UT", 9959 }, // Uttarakhand
        { "IN-WB", 55172 }, // West Bengal
        { "IN-DH", 514 }, // Dadra and Nagar Haveli and Daman and Diu
    };

    private static readonly Dictionary<string, int> MG = new()
    {
        { "MG-U", 15 }, // Toliara
    };

    private static readonly Dictionary<string, int> BW = new()
    {
        { "BW-CE", 3187 }, // Central
        { "BW-CH", 326 }, // Chobe
        { "BW-GH", 586 }, // Ghanzi
        { "BW-KG", 781 }, // Kgalagadi
        { "BW-KL", 142 }, // Kgatleng
        { "BW-KW", 907 }, // Kweneng
        { "BW-NE", 325 }, // North East
        { "BW-NW", 1271 }, // North West
        { "BW-SE", 508 }, // South East
        { "BW-SO", 556 }, // Southern
    };

    private static readonly Dictionary<string, int> GH = new()
    {
        { "GH-AA", 908 }, // Greater Accra
        { "GH-AF", 120 }, // Ahafo
        { "GH-AH", 1218 }, // Ashanti
        { "GH-BE", 75 }, // Bono East
        { "GH-BO", 129 }, // Bono
        { "GH-CP", 661 }, // Central
        { "GH-EP", 638 }, // Eastern
        { "GH-NE", 33 }, // North East
        { "GH-NP", 291 }, // Northern
        { "GH-OT", 153 }, // Oti
        { "GH-SV", 416 }, // Savannah
        { "GH-TV", 379 }, // Volta
        { "GH-UE", 64 }, // Upper East
        { "GH-UW", 528 }, // Upper West
        { "GH-WN", 14 }, // Western North
        { "GH-WP", 561 }, // Western
    };

    private static readonly Dictionary<string, int> LS = new()
    {
        { "LS-A", 597 }, // Maseru
        { "LS-B", 123 }, // Botha-Bothe
        { "LS-C", 289 }, // Leribe
        { "LS-D", 245 }, // Berea
        { "LS-E", 264 }, // Mafeteng
        { "LS-F", 127 }, // Mohale's Hoek
        { "LS-G", 112 }, // Quthing
        { "LS-H", 102 }, // Qacha's Nek
        { "LS-J", 125 }, // Mokhotlong
        { "LS-K", 173 }, // Thaba-Tseka
    };

    private static readonly Dictionary<string, int> RW = new()
    {
        { "RW-01", 1001 }, // Ville de Kigali
        { "RW-02", 584 }, // Est
        { "RW-03", 139 }, // Nord
        { "RW-04", 516 }, // Ouest
        { "RW-05", 339 }, // Sud
    };

    private static readonly Dictionary<string, int> EG = new()
    {
        { "EG-C", 2 }, // Al Qahirah
        { "EG-GZ", 5 }, // Al Jizah
    };

    private static readonly Dictionary<string, int> PN = new()
    {
        { "PN-1", 1 }, // Pitcairn
    };

    private static readonly Dictionary<string, int> AS = new()
    {
        { "US-AS", 1 }, // American Samoa
    };

    private static readonly Dictionary<string, int> GU = new()
    {
        { "US-GU", 1 }, // Guam
    };

    private static readonly Dictionary<string, int> MP = new()
    {
        { "US-MP", 1 }, // Mariana Islands
    };

    private static readonly Dictionary<string, int> QA = new()
    {
        { "QA-DA", 306 }, // Ad Dawhah
        { "QA-KH", 356 }, // Al Khawr wa adh Dhakhirah
        { "QA-MS", 157 }, // Ash Shamal
        { "QA-RA", 602 }, // Ar Rayyan
        { "QA-SH", 454 }, // Ash Shi?aniyah
        { "QA-US", 217 }, // Umm Salal
        { "QA-WA", 403 }, // Al Wakrah
        { "QA-ZA", 257 }, // Az Za'ayin
    };

    private static readonly Dictionary<string, int> AE = new()
    {
        { "AE-AJ", 270 }, // 'Ajman
        { "AE-AZ", 5 }, // Abu Zaby
        { "AE-DU", 1214 }, // Dubayy
        { "AE-FU", 405 }, // Al Fujayrah
        { "AE-RK", 469 }, // Ra's al Khaymah
        { "AE-SH", 1248 }, // Ash Shariqah
        { "AE-UQ", 287 }, // Umm al Qaywayn
    };

    private static readonly Dictionary<string, int> BT = new()
    {
        { "BT-11", 138 }, // Paro
        { "BT-12", 121 }, // Chhukha
        { "BT-13", 43 }, // Haa
        { "BT-15", 108 }, // Thimphu
        { "BT-21", 61 }, // Tsirang
        { "BT-22", 58 }, // Dagana
        { "BT-23", 76 }, // Punakha
        { "BT-24", 143 }, // Wangdue Phodrang
        { "BT-31", 116 }, // Sarpang
        { "BT-32", 135 }, // Trongsa
        { "BT-33", 133 }, // Bumthang
        { "BT-34", 40 }, // Zhemgang
        { "BT-41", 100 }, // Trashigang
        { "BT-42", 158 }, // Monggar
        { "BT-43", 21 }, // Pema Gatshel
        { "BT-44", 28 }, // Lhuentse
        { "BT-45", 49 }, // Samdrup Jongkhar
        { "BT-TY", 37 }, // Trashi Yangtse
    };

    private static readonly Dictionary<string, int> CX = new()
    {
        { "Christmas Island", 1 }, // Christmas Island
    };

    private static readonly Dictionary<string, int> CC = new()
    {
        { "Cocos (Keeling) Islands", 1 }, // Cocos (Keeling) Islands
    };

    private static readonly Dictionary<string, int> HK = new()
    {
        { "CN-HK", 1 }, // Hong Kong
    };

    private static readonly Dictionary<string, int> IL = new()
    {
        { "IL-D", 651 }, // HaDarom
        { "IL-HA", 361 }, // Hefa
        { "IL-JM", 149 }, // Yerushalayim
        { "IL-M", 596 }, // HaMerkaz
        { "IL-TA", 209 }, // Tel Aviv
        { "IL-Z", 610 }, // HaTsafon
    };

    private static readonly Dictionary<string, int> JO = new()
    {
        { "JO-AM", 382 }, // Al 'Asimah
        { "JO-AQ", 213 }, // Al 'Aqabah
        { "JO-AT", 78 }, // At Tafilah
        { "JO-AZ", 1 }, // Az Zarqa'
        { "JO-BA", 40 }, // Al Balqa'
        { "JO-JA", 9 }, // Jarash
        { "JO-KA", 130 }, // Al Karak
        { "JO-MD", 45 }, // Madaba
        { "JO-MN", 180 }, // Ma'an
    };

    private static readonly Dictionary<string, int> JP = new()
    {
        { "JP-01", 22854 }, // Hokkaido
        { "JP-02", 6261 }, // Aomori
        { "JP-03", 9807 }, // Iwate
        { "JP-04", 6966 }, // Miyagi
        { "JP-05", 6846 }, // Akita
        { "JP-06", 5340 }, // Yamagata
        { "JP-07", 10596 }, // Fukushima
        { "JP-08", 8141 }, // Ibaraki
        { "JP-09", 6385 }, // Tochigi
        { "JP-10", 5129 }, // Gunma
        { "JP-11", 5204 }, // Saitama
        { "JP-12", 7393 }, // Chiba
        { "JP-13", 2753 }, // Tokyo
        { "JP-14", 3135 }, // Kanagawa
        { "JP-15", 9192 }, // Niigata
        { "JP-16", 3255 }, // Toyama
        { "JP-17", 3881 }, // Ishikawa
        { "JP-18", 3049 }, // Fukui
        { "JP-19", 2951 }, // Yamanashi
        { "JP-20", 8670 }, // Nagano
        { "JP-21", 6541 }, // Gifu
        { "JP-22", 6960 }, // Shizuoka
        { "JP-23", 6830 }, // Aichi
        { "JP-24", 5235 }, // Mie
        { "JP-25", 3080 }, // Shiga
        { "JP-26", 3759 }, // Kyoto
        { "JP-27", 2706 }, // Osaka
        { "JP-28", 7693 }, // Hyogo
        { "JP-29", 2658 }, // Nara
        { "JP-30", 3695 }, // Wakayama
        { "JP-31", 2983 }, // Tottori
        { "JP-32", 5532 }, // Shimane
        { "JP-33", 7293 }, // Okayama
        { "JP-34", 8065 }, // Hiroshima
        { "JP-35", 5833 }, // Yamaguchi
        { "JP-36", 3226 }, // Tokushima
        { "JP-37", 2383 }, // Kagawa
        { "JP-38", 4884 }, // Ehime
        { "JP-39", 4353 }, // Kochi
        { "JP-40", 6178 }, // Fukuoka
        { "JP-41", 3207 }, // Saga
        { "JP-42", 4668 }, // Nagasaki
        { "JP-43", 7029 }, // Kumamoto
        { "JP-44", 5975 }, // Oita
        { "JP-45", 5668 }, // Miyazaki
        { "JP-46", 9724 }, // Kagoshima
        { "JP-47", 2525 }, // Okinawa
    };

    private static readonly Dictionary<string, int> KG = new()
    {
        { "KG-C", 590 }, // Chuy
        { "KG-GB", 213 }, // Bishkek Shaary
        { "KG-GO", 47 }, // Osh Shaary
        { "KG-J", 443 }, // Jalal-Abad
        { "KG-N", 359 }, // Naryn
        { "KG-O", 97 }, // Osh
        { "KG-T", 126 }, // Talas
        { "KG-Y", 441 }, // Ysyk-Kol
    };

    private static readonly Dictionary<string, int> KH = new()
    {
        { "KH-1", 1856 }, // Banteay Mean Choay
        { "KH-10", 512 }, // Kracheh
        { "KH-11", 473 }, // Mondol Kiri
        { "KH-12", 928 }, // Phnom Penh
        { "KH-13", 813 }, // Preah Vihear
        { "KH-14", 2626 }, // Prey Veaeng
        { "KH-15", 1176 }, // Pousaat
        { "KH-16", 554 }, //
        { "KH-17", 3359 }, // Siem Reab
        { "KH-18", 710 }, // Preah Sihanouk
        { "KH-19", 523 }, // Stueng Traeng
        { "KH-2", 3426 }, // Baat Dambang
        { "KH-20", 1912 }, // Svaay Rieng
        { "KH-21", 2301 }, // Taakaev
        { "KH-22", 441 }, //
        { "KH-23", 164 }, // Kaeb
        { "KH-24", 417 }, // Pailin
        { "KH-25", 2042 }, //
        { "KH-3", 1796 }, // Kampong Chaam
        { "KH-4", 968 }, // Kampong Chhnang
        { "KH-5", 3056 }, // Kampong Spueu
        { "KH-6", 854 }, // Kampong Thum
        { "KH-7", 2485 }, // Kampot
        { "KH-8", 2121 }, // Kandaal
        { "KH-9", 857 }, //
    };

    private static readonly Dictionary<string, int> KR = new()
    {
        { "KR-11", 891 }, // Seoul-teukbyeolsi
        { "KR-26", 836 }, // Busan-gwangyeoksi
        { "KR-27", 791 }, // Daegu-gwangyeoksi
        { "KR-28", 1071 }, // Incheon-gwangyeoksi
        { "KR-29", 37 }, // Gwangju-gwangyeoksi
        { "KR-30", 534 }, // Daejeon-gwangyeoksi
        { "KR-31", 91 }, // Ulsan-gwangyeoksi
        { "KR-41", 5919 }, // Gyeonggi-do
        { "KR-42", 3599 }, // Gangwon-do
        { "KR-43", 123 }, // Chungcheongbuk-do
        { "KR-44", 415 }, // Chungcheongnam-do
        { "KR-45", 1536 }, // Jeollabuk-do
        { "KR-46", 2782 }, // Jeollanam-do
        { "KR-47", 5391 }, // Gyeongsangbuk-do
        { "KR-48", 1857 }, // Gyeongsangnam-do
        { "KR-49", 1589 }, // Jeju-teukbyeoljachido
        { "KR-50", 4 }, // Sejong
    };

    private static readonly Dictionary<string, int> SG = new()
    {
        { "SG-01", 190 }, // Central Singapore
        { "SG-02", 112 }, // North East
        { "SG-03", 159 }, // North West
        { "SG-04", 159 }, // South East
        { "SG-05", 280 }, // South West
    };

    private static readonly Dictionary<string, int> TW = new()
    {
        { "TW-CHA", 1789 }, // Changhua
        { "TW-CYI", 111 }, //
        { "TW-CYQ", 2039 }, // Chiayi
        { "TW-HSQ", 1120 }, // Hsinchu
        { "TW-HSZ", 188 }, //
        { "TW-HUA", 1494 }, // Hualien
        { "TW-ILA", 966 }, // Yilan
        { "TW-KEE", 180 }, // Keelung
        { "TW-KHH", 2110 }, // Kaohsiung
        { "TW-KIN", 156 }, // Kinmen
        { "TW-LIE", 50 }, // Lienchiang
        { "TW-MIA", 1650 }, // Miaoli
        { "TW-NAN", 2213 }, // Nantou
        { "TW-NWT", 1957 }, // New Taipei
        { "TW-PEN", 186 }, // Penghu
        { "TW-PIF", 2166 }, // Pingtung
        { "TW-TAO", 1450 }, // Taoyuan
        { "TW-TNN", 3052 }, // Tainan
        { "TW-TPE", 414 }, // Taipei
        { "TW-TTT", 1496 }, // Taitung
        { "TW-TXG", 1966 }, // Taichung
        { "TW-YUN", 1977 }, // Yunlin
    };

    private static readonly Dictionary<string, int> BM = new()
    {
        { "BM-1", 1 }, // Bermuda
    };

    private static readonly Dictionary<string, int> VI = new()
    {
        { "US-VI", 1 }, // U.S. Virgin Islands
    };

    private static readonly Dictionary<string, int> GL = new()
    {
        { "GL-AV", 30 }, // Avannaata Kommunia
        { "GL-KU", 38 }, // Kommune Kujalleq
        { "GL-QE", 40 }, // Qeqqata Kommunia
        { "GL-QT", 19 }, // Kommune Qeqertalik
        { "GL-SM", 53 }, // Kommuneqarfik Sermersooq
    };

    private static readonly Dictionary<string, int> CR = new()
    {
        { "CR-A", 2 }, // Alajuela
        { "CR-C", 2 }, // Cartago
        { "CR-G", 4 }, // Guanacaste
        { "CR-H", 3 }, // Heredia
        { "CR-SJ", 11 }, // San Jose
    };

    private static readonly Dictionary<string, int> UG = new()
    {
        { "UG-C", 971 }, //
        { "UG-N", 290 }, //
        { "UG-W", 391 }, //
    };

    private static readonly Dictionary<string, int> ML = new()
    {
        { "ML-10", 200 }, //
    };

    private static readonly Dictionary<string, int> RE = new()
    {
        { "FR-974", 610 }, //
    };

    private static readonly Dictionary<string, int> MN = new()
    {
        { "MN-035", 76 }, // Orhon
        { "MN-037", 88 }, // Darhan uul
        { "MN-039", 294 }, //
        { "MN-041", 468 }, //
        { "MN-043", 345 }, // Hovd
        { "MN-046", 194 }, //
        { "MN-047", 624 }, // Tov
        { "MN-049", 526 }, // Selenge
        { "MN-051", 442 }, //
        { "MN-053", 146 }, // Omnogovi
        { "MN-055", 108 }, // Ovorhangay
        { "MN-057", 280 }, //
        { "MN-059", 418 }, //
        { "MN-061", 601 }, // Dornod
        { "MN-063", 371 }, //
        { "MN-064", 49 }, //
        { "MN-065", 43 }, // Govi-Altay
        { "MN-067", 114 }, // Bulgan
        { "MN-069", 13 }, // Bayanhongor
        { "MN-071", 233 }, //
        { "MN-073", 301 }, //
        { "MN-1", 450 }, // Ulaanbaatar
    };

    private static readonly Dictionary<string, int> MO = new()
    {
        { "CN-MO", 57 }, //
    };

    private static readonly Dictionary<string, int> BY = new()
    {
        { "BY-HM", 1 }, // Horad Minsk
    };

    private static readonly Dictionary<string, int> MQ = new()
    {
        { "FR-972", 5 }, //
    };

    private static readonly Dictionary<string, int> PK = new()
    {
        { "PK-PB", 7 }, // Punjab
    };

    private static readonly Dictionary<string, int> PS = new()
    {
        { "Area A", 97 }, //
        { "Area B", 16 }, //
        { "Judea and Samaria", 500 }, //
    };

    private static readonly Dictionary<string, int> JE = new()
    {
        { "JE-1", 196 }, //
    };

    private static readonly Dictionary<string, int> IM = new()
    {
        { "IM-1", 562 }, //
    };

    private static readonly Dictionary<string, int> UM = new()
    {
        { "UM-1", 5 }, //
    };

    private static readonly Dictionary<string, int> NA = new()
    {
        { "NA-ER", 10 }, //	Erongo
        { "NA-HA", 10 }, //	Hardap
        { "NA-KA", 10 }, //	Karas
        { "NA-KE", 10 }, //	Kavango East
        { "NA-KW", 10 }, //	Kavango West
        { "NA-KH", 10 }, //	Khomas
        { "NA-KU", 10 }, //	Kunene
        { "NA-OW", 10 }, //	Ohangwena
        { "NA-OH", 10 }, //	Omaheke
        { "NA-OS", 10 }, //	Omusati
        { "NA-ON", 10 }, //	Oshana
        { "NA-OT", 10 }, //	Oshikoto
        { "NA-OD", 10 }, //	Otjozondjupa
        { "NA-CA", 10 }, //	Zambezi
    };

    private static readonly Dictionary<string, int> OM = new()
    {
        { "OM-DA", 10 }, // Interior
        { "OM-BU", 10 }, // Buraymi
        { "OM-WU", 10 }, // Central
        { "OM-ZA", 10 }, // Dhahira
        { "OM-BJ", 10 }, // South Batina
        { "OM-SJ", 10 }, // Southeastern
        { "OM-MA", 10 }, // Muscat
        { "OM-MU", 10 }, // Musandam
        { "OM-BS", 10 }, // North Batina
        { "OM-SS", 10 }, // Northeastern
        { "OM-ZU", 10 }, // Dhofar
    };

    private static readonly Dictionary<string, int> KZ = new()
    {
        { "KZ-10", 561 }, // Abay oblysy
        { "KZ-75", 517 }, // Almaty
        { "KZ-19", 677 }, // Almaty oblysy
        { "KZ-11", 1416 }, // Aqmola oblysy
        { "KZ-15", 1355 }, // Aqtobe oblysy
        { "KZ-71", 326 }, // Astana
        { "KZ-23", 559 }, // Atyrau oblysy
        { "KZ-27", 534 }, // Batys Qazaqstan oblysy
        { "KZ-47", 837 }, // Mangghystau oblysy
        { "KZ-55", 1149 }, // Pavlodar oblysy
        { "KZ-35", 1316 }, // Qaraghandy oblysy
        { "KZ-39", 744 }, // Qostanay oblysy
        { "KZ-43", 551 }, // Qyzylorda oblysy
        { "KZ-63", 610 }, // Shyghys Qazaqstan oblysy
        { "KZ-79", 612 }, // Shymkent
        { "KZ-59", 676 }, // Soltustik Qazaqstan oblysy
        { "KZ-61", 733 }, // Turkistan oblysy
        { "KZ-62", 263 }, // Ulytau oblysy
        { "KZ-31", 697 }, // Zhambyl oblysy
        { "KZ-33", 795 }, // Zhetisu oblysy
    };

    private static readonly Dictionary<string, int> ST = new()
    {
        { "ST-01", 41 }, // Agua Grande
        { "ST-02", 34 }, //
        { "ST-03", 41 }, //
        { "ST-04", 24 }, //
        { "ST-05", 75 }, //
        { "ST-06", 57 }, //
        { "ST-P", 36 }, //
    };

    private static readonly Dictionary<string, int> LB = new()
    {
        { "LB-BH", 1 }, // Baalbek-Hermel
        { "LB-BI", 19 }, // Beqaa
        { "LB-BA", 13 }, // Beyrouth
        { "LB-AS", 5 }, // Liban-Nord
        { "LB-JA", 1 }, // Liban-Sud
        { "LB-JL", 28 }, // Mont-Liban
        { "LB-NA", 1 }, // Nabatiye
    };

    private static readonly Dictionary<string, int> PY = new()
    {
        { "PY-16", 10 }, // Alto Paraguay	Upper Paraguay	department
        { "PY-10", 10 }, // Alto Paraná	Upper Parana	department
        { "PY-13", 10 }, // Amambay	Amambay	department
        { "PY-ASU", 10 }, // Asunción	Asuncion	capital
        { "PY-19", 10 }, // Boquerón	Boqueron	department
        { "PY-5", 10 }, // Caaguazú	Caaguazu	department
        { "PY-6", 10 }, // Caazapá	Caazapa	department
        { "PY-14", 10 }, // Canindeyú	Canindeyu	department
        { "PY-11", 10 }, // Central	Central	department
        { "PY-1", 10 }, // Concepción	Concepcion	department
        { "PY-3", 10 }, // Cordillera	Cordillera	department
        { "PY-4", 10 }, // Guairá	Guaira	department
        { "PY-7", 10 }, // Itapúa	Itapua	department
        { "PY-8", 10 }, // Misiones	Misiones	department
        { "PY-12", 10 }, // Ñeembucú	Neembucu	department
        { "PY-9", 10 }, // Paraguarí	Paraguari	department
        { "PY-15", 10 }, // Presidente Hayes	President Hayes	department
        { "PY-2", 10 }, // San Pedro	Saint Peter	department
    };

    private static readonly Dictionary<string, int> BA = new()
    {
        { "BA-BIH", 10 }, // Federacija Bosne i Hercegovine	Federation of Bosnia and Herzegovina	entity
        { "BA-SRP", 10 }, // Republika Srpska	Republika Srpska	entity
        { "BA-BRC", 10 }, // Brčko distrikt	Brčko District	district with special status
    };

    private static readonly Dictionary<string, int> VN = new()
    {
        { "VN-44", 10 }, // An Giang	province
        { "VN-43", 10 }, // Bà Rịa - Vũng Tàu	province
        { "VN-54", 10 }, // Bắc Giang	province
        { "VN-53", 10 }, // Bắc Kạn	province
        { "VN-55", 10 }, // Bạc Liêu	province
        { "VN-56", 10 }, // Bắc Ninh	province
        { "VN-50", 10 }, // Bến Tre	province
        { "VN-31", 10 }, // Bình Định	province
        { "VN-57", 10 }, // Bình Dương	province
        { "VN-58", 10 }, // Bình Phước	province
        { "VN-40", 10 }, // Bình Thuận	province
        { "VN-59", 10 }, // Cà Mau	province
        { "VN-CT", 10 }, // Cần Thơ	municipality
        { "VN-04", 10 }, // Cao Bằng	province
        { "VN-DN", 10 }, // Đà Nẵng	municipality
        { "VN-33", 10 }, // Đắk Lắk	province
        { "VN-72", 10 }, // Đắk Nông	province
        { "VN-71", 10 }, // Điện Biên	province
        { "VN-39", 10 }, // Đồng Nai	province
        { "VN-45", 10 }, // Đồng Tháp	province
        { "VN-30", 10 }, // Gia Lai	province
        { "VN-03", 10 }, // Hà Giang	province
        { "VN-63", 10 }, // Hà Nam	province
        { "VN-HN", 10 }, // Hà Nội	municipality
        { "VN-23", 10 }, // Hà Tĩnh	province
        { "VN-61", 10 }, // Hải Dương	province
        { "VN-HP", 10 }, // Hải Phòng	municipality
        { "VN-73", 10 }, // Hậu Giang	province
        { "VN-SG", 10 }, // Hồ Chí Minh
        { "VN-14", 10 }, // Hòa Bình	province
        { "VN-66", 10 }, // Hưng Yên	province
        { "VN-34", 10 }, // Khánh Hòa	province
        { "VN-47", 10 }, // Kiến Giang	province
        { "VN-28", 10 }, // Kon Tum	province
        { "VN-01", 10 }, // Lai Châu	province
        { "VN-35", 10 }, // Lâm Đồng	province
        { "VN-09", 10 }, // Lạng Sơn	province
        { "VN-02", 10 }, // Lào Cai	province
        { "VN-41", 10 }, // Long An	province
        { "VN-67", 10 }, // Nam Định	province
        { "VN-22", 10 }, // Nghệ An	province
        { "VN-18", 10 }, // Ninh Bình	province
        { "VN-36", 10 }, // Ninh Thuận	province
        { "VN-68", 10 }, // Phú Thọ	province
        { "VN-32", 10 }, // Phú Yên	province
        { "VN-24", 10 }, // Quảng Bình	province
        { "VN-27", 10 }, // Quảng Nam	province
        { "VN-29", 10 }, // Quảng Ngãi	province
        { "VN-13", 10 }, // Quảng Ninh	province
        { "VN-25", 10 }, // Quảng Trị	province
        { "VN-52", 10 }, // Sóc Trăng	province
        { "VN-05", 10 }, // Sơn La	province
        { "VN-37", 10 }, // Tây Ninh	province
        { "VN-20", 10 }, // Thái Bình	province
        { "VN-69", 10 }, // Thái Nguyên	province
        { "VN-21", 10 }, // Thanh Hóa	province
        { "VN-26", 10 }, // Thừa Thiên-Huế	province
        { "VN-46", 10 }, // Tiền Giang	province
        { "VN-51", 10 }, // Trà Vinh	province
        { "VN-07", 10 }, // Tuyên Quang	province
        { "VN-49", 10 }, // Vĩnh Long	province
        { "VN-70", 10 }, // Vĩnh Phúc	province
        { "VN-06", 10 }, // Yên Bái	province
    };

    private static readonly Dictionary<string, int> LI = new()
    {
        { "LI-01", 10 }, // Balzers
        { "LI-02", 10 }, // Eschen
        { "LI-03", 10 }, // Gamprin
        { "LI-04", 10 }, // Mauren
        { "LI-05", 10 }, // Planken
        { "LI-06", 10 }, // Ruggell
        { "LI-07", 10 }, // Schaan
        { "LI-08", 10 }, // Schellenberg
        { "LI-09", 10 }, // Triesen
        { "LI-10", 10 }, // Triesenberg
        { "LI-11", 10 }, // Vaduz
    };

    public static readonly Dictionary<string, Dictionary<string, int>> CountryToSubdivision =
        new()
        {
            { "DK", DK },
            { "NZ", NZ },
            { "NL", NL },
            { "US", US },
            { "IT", IT },
            { "FR", FR },
            { "LU", LU },
            { "AU", AU },
            { "NO", NO },
            { "SE", SE },
            { "HR", HR },
            { "EE", EE },
            { "MX", MX },
            { "PR", PR },
            { "DO", DO },
            { "CW", CW },
            { "PA", PA },
            { "CO", CO },
            { "EC", EC },
            { "PE", PE },
            { "BO", BO },
            { "CL", CL },
            { "AR", AR },
            { "BR", BR },
            { "UY", UY },
            { "GT", GT },
            { "ID", ID },
            { "CA", CA },
            { "GR", GR },
            { "HU", HU },
            { "IS", IS },
            { "FI", FI },
            { "LV", LV },
            { "LT", LT },
            { "PL", PL },
            { "SZ", SZ },
            { "AT", AT },
            { "ES", ES },
            { "DE", DE },
            { "CZ", CZ },
            { "SK", SK },
            { "BE", BE },
            { "PT", PT },
            { "RO", RO },
            { "BG", BG },
            { "MK", MK },
            { "ME", ME },
            { "RS", RS },
            { "SI", SI },
            { "CH", CH },
            { "AD", AD },
            { "SM", SM },
            { "MC", MC },
            { "GI", GI },
            { "IE", IE },
            { "GB", GB },
            { "TR", TR },
            { "UA", UA },
            { "AL", AL },
            { "FO", FO },
            { "MT", MT },
            { "RU", RU },
            { "NG", NG },
            { "SN", SN },
            { "TN", TN },
            { "KE", KE },
            { "ZA", ZA },
            { "BD", BD },
            { "MY", MY },
            { "TH", TH },
            { "LA", LA },
            { "PH", PH },
            { "LK", LK },
            { "IN", IN },
            { "MG", MG },
            { "BW", BW },
            { "GH", GH },
            { "LS", LS },
            { "RW", RW },
            { "EG", EG },
            { "PN", PN },
            { "AS", AS },
            { "GU", GU },
            { "MP", MP },
            { "QA", QA },
            { "AE", AE },
            { "BT", BT },
            { "CX", CX },
            { "CC", CC },
            { "HK", HK },
            { "IL", IL },
            { "JO", JO },
            { "JP", JP },
            { "KG", KG },
            { "KH", KH },
            { "KR", KR },
            { "SG", SG },
            { "TW", TW },
            { "BM", BM },
            { "VI", VI },
            { "GL", GL },
            { "CR", CR },
            { "UG", UG },
            { "ML", ML },
            { "RE", RE },
            { "LB", LB },
            { "MN", MN },
            { "MO", MO },
            { "BY", BY },
            { "MQ", MQ },
            { "PK", PK },
            { "PS", PS },
            { "JE", JE },
            { "IM", IM },
            { "UM", UM },
            { "KZ", KZ },
            { "ST", ST },
        };

    public static readonly Dictionary<string, Dictionary<string, int>> NotQuiteThereYetCountryToSubdivision =
        new()
        {
            { "NA", NA },
            { "OM", OM },
            { "PY", PY },
            { "BA", BA },
            { "VN", VN },
            { "LI", LI },
        };

    private static Dictionary<string, SubdivisionInfo[]>? _subdivisions;

    public static int GoalForSubdivision(string countryCode, string subdivisionCode, int totalGoalCount, string[]? availableSubdivisions = null)
    {
        var regionTotalWeight = CountryToSubdivision.TryGetValue(countryCode, out var weights)
            ? weights.Where(w => availableSubdivisions == null || availableSubdivisions.Any(a => a == w.Key)).Sum(x => x.Value)
            : (int?)null;

        if (regionTotalWeight == null)
        {
            throw new InvalidOperationException($"Weight for subdivision {subdivisionCode} is null.");
        }

        if (weights == null || !weights.TryGetValue(subdivisionCode, out int value))
        {
            throw new InvalidOperationException($"Subdivision code {subdivisionCode} is not defined.");
        }

        var regionGoalCount = ((decimal)value / regionTotalWeight * totalGoalCount).Value.RoundToInt();
        return regionGoalCount;
    }

    public static string SubdivisionName(string countryCode, string code) =>
        GetSubdivisions()[countryCode].FirstOrDefault(x => x.SubdivisionCode == code)?.Name ?? "N/A";

    public static (string subdivisionCode, string file)[] AllSubdivisionFiles(string countryCode, RunMode runMode)
    {
        var subdivisionKeys = CountryToSubdivision[countryCode].Where(x => x.Value > 0).Select(x => x.Key);
        var folder = DataDownloadService.CountryFolder(countryCode, runMode);
        return subdivisionKeys.Select(x => (x, Path.Combine(folder, $"{countryCode}+{x}.bin"))).ToArray();
    }

    public static Dictionary<string, SubdivisionInfo[]> GetSubdivisions()
    {
        if (_subdivisions != null)
        {
            return _subdivisions;
        }

        var resource = Extensions.ReadManifestData("subdivisions.json");
        var subdivisions  = Serializer.Deserialize<SubdivisionInfo[]>(resource) ?? throw new InvalidOperationException("No subdivisions found in subdivisions.json file");
        _subdivisions = subdivisions.GroupBy(x => x.CountryCode).ToDictionary(x => x.Key, x => x.ToArray());
        return _subdivisions;
    }

    public record SubdivisionInfo
    {
        public required string CountryCode { get; set; }
        public required string SubdivisionCode { get; set; }
        public string? Name { get; set; }
        public int Area { get; set; }
        public int Inhabitants { get; set; }
    }
}