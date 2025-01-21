namespace Vali.Core;

public class SubdivisionWeights
{

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AD = new()
    {
        { "AD-02", (1650, "Canillo") },
        { "AD-03", (1123, "Encamp") },
        { "AD-04", (1057, "La Massana") },
        { "AD-05", (1186, "Ordino") },
        { "AD-06", (1003, "Sant Julia de Loria") },
        { "AD-07", (418, "Andorra la Vella") },
        { "AD-08", (562, "Escaldes-Engordany") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AE = new()
    {
        { "AE-AJ", (270, "Imarat ‘Ajman") },
        { "AE-AZ", (5, "Abu Dhabi") },
        { "AE-DU", (1214, "Dubai") },
        { "AE-FU", (405, "Imarat al Fujayrah") },
        { "AE-RK", (469, "Imarat Ra’s al Khaymah") },
        { "AE-SH", (1248, "Ash Shariqah") },
        { "AE-UQ", (287, "Imarat Umm al Qaywayn") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AL = new()
    {
        { "AL-01", (672, "Qarku i Beratit") },
        { "AL-02", (468, "Qarku i Durresit") },
        { "AL-03", (1284, "Qarku i Elbasanit") },
        { "AL-04", (829, "Qarku i Fierit") },
        { "AL-05", (1054, "Qarku i Gjirokastres") },
        { "AL-06", (1435, "Qarku i Korces") },
        { "AL-07", (880, "Qarku i Kukesit") },
        { "AL-08", (661, "Qarku i Lezhes") },
        { "AL-09", (960, "Qarku i Dibres") },
        { "AL-10", (1446, "Qarku i Shkodres") },
        { "AL-11", (1189, "Tirana") },
        { "AL-12", (1120, "Qarku i Vlores") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AR = new()
    {
        { "AR-A", (5061, "Salta") },
        { "AR-B", (7573, "Buenos Aires") },
        { "AR-C", (421, "Buenos Aires city") },
        { "AR-D", (4051, "San Luis") },
        { "AR-E", (4170, "Entre Rios") },
        { "AR-F", (4490, "La Rioja") },
        { "AR-G", (4580, "Santiago del Estero") },
        { "AR-H", (4516, "Chaco") },
        { "AR-J", (4504, "San Juan") },
        { "AR-K", (4485, "Catamarca") },
        { "AR-L", (4743, "La Pampa") },
        { "AR-M", (5356, "Mendoza") },
        { "AR-N", (3682, "Misiones") },
        { "AR-P", (3421, "Formosa") },
        { "AR-Q", (4974, "Neuquen") },
        { "AR-R", (6210, "Rio Negro") },
        { "AR-S", (4832, "Santa Fe") },
        { "AR-T", (3270, "Tucuman") },
        { "AR-U", (6577, "Chubut") },
        { "AR-V", (3170, "Tierra del Fuego") },
        { "AR-W", (4416, "Corrientes") },
        { "AR-X", (5620, "Cordoba") },
        { "AR-Y", (3770, "Jujuy") },
        { "AR-Z", (7132, "Santa Cruz") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AS = new()
    {
        { "US-AS", (1, "American Samoa") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AT = new()
    {
        { "AT-1", (415, "Burgenland") },
        { "AT-2", (945, "Karnten") },
        { "AT-3", (2025, "Niederosterreich") },
        { "AT-4", (1372, "Oberosterreich") },
        { "AT-5", (724, "Salzburg") },
        { "AT-6", (1701, "Steiermark") },
        { "AT-7", (1239, "Tirol") },
        { "AT-8", (309, "Vorarlberg") },
        { "AT-9", (269, "Vienna") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AU = new()
    {
        { "AU-ACT", (600, "Australian Capital Territory") },
        { "AU-NSW", (8735, "New South Wales") },
        { "AU-NT", (4366, "Northern Territory") },
        { "AU-QLD", (8735, "Queensland") },
        { "AU-SA", (8735, "South Australia") },
        { "AU-TAS", (4850, "Tasmania") },
        { "AU-VIC", (8735, "Victoria") },
        { "AU-WA", (8735, "Western Australia") },
        { "Jervis Bay Territory", (1, "Jervis Bay Territory") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BA = new()
    {
        { "BA-BIH", (10, "Federation of Bosnia and Herzegovina") },
        { "BA-SRP", (10, "Republika Srpska") },
        { "BA-BRC", (10, "Brčko District") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BD = new()
    {
        { "BD-A", (2601, "Barisal") },
        { "BD-B", (7641, "Chittagong") },
        { "BD-C", (9188, "Dhaka") },
        { "BD-D", (8944, "Khulna") },
        { "BD-E", (5112, "Rajshahi") },
        { "BD-F", (2903, "Rangpur") },
        { "BD-G", (3573, "Sylhet") },
        { "BD-H", (4073, "Mymensingh") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BE = new()
    {
        { "BE-VAN", (1076, "Antwerpen") },
        { "BE-VBR", (793, "Vlaams-Brabant") },
        { "BE-VLI", (839, "Limburg") },
        { "BE-VOV", (1104, "Oost-Vlaanderen") },
        { "BE-VWV", (1107, "West-Vlaanderen") },
        { "BE-WBR", (382, "Brabant wallon") },
        { "BE-WHT", (1312, "Hainaut") },
        { "BE-WLG", (1282, "Liege") },
        { "BE-WLX", (819, "Luxembourg") },
        { "BE-WNA", (1140, "Namurc") },
        { "BE-BRU", (80, "Brussels") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BG = new()
    {
        { "BG-01", (1545, "Blagoevgrad") },
        { "BG-02", (1861, "Burgas") },
        { "BG-03", (1055, "Varna") },
        { "BG-04", (1146, "Oblast Veliko Tarnovo") },
        { "BG-05", (708, "Oblast Vidin") },
        { "BG-06", (859, "Oblast Vratsa") },
        { "BG-07", (567, "Gabrovo") },
        { "BG-08", (1096, "Oblast Dobrich") },
        { "BG-09", (852, "Oblast Kardzhali") },
        { "BG-10", (755, "Oblast Kyustendil") },
        { "BG-11", (961, "Lovech") },
        { "BG-12", (856, "Oblast Montana") },
        { "BG-13", (1063, "Pazardzhik") },
        { "BG-14", (602, "Pernik") },
        { "BG-15", (1113, "Oblast Pleven") },
        { "BG-16", (1635, "Plovdiv") },
        { "BG-17", (568, "Oblast Razgrad") },
        { "BG-18", (743, "Oblast Ruse") },
        { "BG-19", (665, "Oblast Silistra") },
        { "BG-20", (864, "Oblast Sliven") },
        { "BG-21", (763, "Oblast Smolyan") },
        { "BG-22", (978, "Sofia-Grad") },
        { "BG-23", (1796, "Sofia") },
        { "BG-24", (1292, "Oblast Stara Zagora") },
        { "BG-25", (660, "Oblast Targovishte") },
        { "BG-26", (1388, "Haskovo") },
        { "BG-27", (827, "Oblast Shumen") },
        { "BG-28", (781, "Oblast Yambol") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BM = new()
    {
        { "BM-1", (1, "Bermuda") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AX = new()
    {
        { "AX-1", (1, "Åland Islands") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BO = new()
    {
        { "BO-C", (1288, "Departamento de Cochabamba") },
        { "BO-H", (622, "Departamento de Chuquisaca") },
        { "BO-L", (1158, "Departamento de La Paz") },
        { "BO-O", (632, "Departamento de Oruro") },
        { "BO-P", (651, "Departamento de Potosi") },
        { "BO-S", (1840, "Departamento de Santa Cruz") },
        { "BO-T", (20, "Departamento de Tarija") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BR = new()
    {
        { "BR-AC", (1349, "Acre") },
        { "BR-AL", (1437, "Alagoas") },
        { "BR-AM", (1191, "Amazonas") },
        { "BR-AP", (1180, "Amapa") },
        { "BR-BA", (9413, "Bahia") },
        { "BR-CE", (3790, "Ceara") },
        { "BR-DF", (250, "Federal District") },
        { "BR-ES", (2048, "Espirito Santo") },
        { "BR-GO", (6448, "Goias") },
        { "BR-MA", (3779, "Maranhao") },
        { "BR-MG", (10537, "Minas Gerais") },
        { "BR-MS", (4510, "Mato Grosso do Sul") },
        { "BR-MT", (6126, "Mato Grosso") },
        { "BR-PA", (3751, "Para") },
        { "BR-PB", (2021, "Paraiba") },
        { "BR-PE", (2736, "Pernambuco") },
        { "BR-PI", (3729, "Piaui") },
        { "BR-PR", (5913, "Parana") },
        { "BR-RJ", (2021, "Rio de Janeiro") },
        { "BR-RN", (2032, "Rio Grande do Norte") },
        { "BR-RO", (2190, "Rondonia") },
        { "BR-RR", (1715, "Roraima") },
        { "BR-RS", (4510, "Rio Grande do Sul") },
        { "BR-SC", (2992, "Santa Catarina") },
        { "BR-SE", (1355, "Sergipe") },
        { "BR-SP", (7081, "Sao Paulo") },
        { "BR-TO", (4133, "Tocantins") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BT = new()
    {
        { "BT-11", (138, "Paro Dzongkhag") },
        { "BT-12", (121, "Chhukha Dzongkhag") },
        { "BT-13", (43, "Haa Dzongkhag") },
        { "BT-15", (108, "Thimphu Dzongkhag") },
        { "BT-21", (61, "Tsirang Dzongkhag") },
        { "BT-22", (58, "Dagana Dzongkhag") },
        { "BT-23", (76, "Punakha Dzongkhag") },
        { "BT-24", (143, "Wangdue Phodrang Dzongkhag") },
        { "BT-31", (116, "Sarpang Dzongkhag") },
        { "BT-32", (135, "Trongsa Dzongkhag") },
        { "BT-33", (133, "Bumthang Dzongkhag") },
        { "BT-34", (40, "Zhemgang Dzongkhag") },
        { "BT-41", (100, "Trashigang Dzongkhag") },
        { "BT-42", (158, "Mongar Dzongkhag") },
        { "BT-43", (21, "Pemagatshel Dzongkhag") },
        { "BT-44", (28, "Lhuentse Dzongkhag") },
        { "BT-45", (49, "Samdrup Jongkhar Dzongkhag") },
        { "BT-TY", (37, "Trashi Yangste") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BW = new()
    {
        { "BW-CE", (3187, "Central") },
        { "BW-CH", (326, "Chobe") },
        { "BW-GH", (586, "Ghanzi") },
        { "BW-KG", (781, "Kgalagadi") },
        { "BW-KL", (142, "Kgatleng") },
        { "BW-KW", (907, "Kweneng") },
        { "BW-NE", (325, "North East") },
        { "BW-NW", (1271, "North West") },
        { "BW-SE", (508, "Gaborone") },
        { "BW-SO", (556, "Southern") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CA = new()
    {
        { "CA-AB", (5366, "Alberta") },
        { "CA-BC", (6706, "British Columbia") },
        { "CA-MB", (4471, "Manitoba") },
        { "CA-NB", (3219, "New Brunswick") },
        { "CA-NL", (4471, "Newfoundland and Labrador") },
        { "CA-NS", (3219, "Nova Scotia") },
        { "CA-NT", (824, "Northwest Territories") },
        { "CA-ON", (8585, "Ontario") },
        { "CA-PE", (1072, "Prince Edward Island") },
        { "CA-QC", (7512, "Quebec") },
        { "CA-SK", (4471, "Saskatchewan") },
        { "CA-YT", (2489, "Yukon") },
        { "CA-NU", (88, "Nunavut") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CC = new()
    {
        { "Cocos (Keeling) Islands", (1, "Cocos (Keeling) Islands") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CH = new()
    {
        { "CH-AG", (220, "Kanton Aargau") },
        { "CH-AI", (25, "Kanton Appenzell Innerrhoden") },
        { "CH-AR", (47, "Kanton Appenzell Ausserrhoden") },
        { "CH-BE", (575, "Canton de Berne") },
        { "CH-BL", (86, "Kanton Basel-Landschaft") },
        { "CH-BS", (9, "Kanton Basel-Stadt") },
        { "CH-FR", (204, "Canton de Fribourg") },
        { "CH-GE", (44, "Canton de Geneve") },
        { "CH-GL", (35, "Kanton Glarus") },
        { "CH-GR", (362, "Kanton Graubunden") },
        { "CH-JU", (110, "Canton de Jura") },
        { "CH-LU", (201, "Kanton Luzern") },
        { "CH-NE", (106, "Canton de Neuchatel") },
        { "CH-NW", (25, "Kanton Nidwalden") },
        { "CH-OW", (39, "Kanton Obwalden") },
        { "CH-SG", (240, "Kanton St. Gallen") },
        { "CH-SH", (51, "Kanton Schaffhausen") },
        { "CH-SO", (131, "Kanton Solothurn") },
        { "CH-SZ", (92, "Kanton Schwyz") },
        { "CH-TG", (141, "Kanton Thurgau") },
        { "CH-TI", (179, "Cantone Ticino") },
        { "CH-UR", (45, "Kanton Uri") },
        { "CH-VD", (380, "Canton de Vaud") },
        { "CH-VS", (226, "Canton du Valais") },
        { "CH-ZG", (36, "Kanton Zug") },
        { "CH-ZH", (251, "Kanton Zurich") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CL = new()
    {
        { "CL-AI", (950, "Region Aysen") },
        { "CL-AN", (900, "Antofagasta") },
        { "CL-AP", (300, "Arica and Parinacota") },
        { "CL-AR", (400, "Araucania") },
        { "CL-AT", (700, "Atacama") },
        { "CL-BI", (250, "Biobio") },
        { "CL-CO", (600, "Coquimbo") },
        { "CL-LI", (250, "O'Higgins") },
        { "CL-LL", (800, "Lagos") },
        { "CL-LR", (300, "Rios") },
        { "CL-MA", (600, "Magellan") },
        { "CL-ML", (450, "Maule") },
        { "CL-NB", (200, "Nuble") },
        { "CL-RM", (350, "Region Metropolitana") },
        { "CL-TA", (500, "Tarapaca") },
        { "CL-VS", (450, "Valparaiso") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CO = new()
    {
        { "CO-AMA", (0, "Amazonas") },
        { "CO-ANT", (4335, "Antioquia") },
        { "CO-ARA", (1186, "Arauca") },
        { "CO-ATL", (1717, "Atlantico") },
        { "CO-BOL", (1988, "Bolivar") },
        { "CO-BOY", (2569, "Boyaca") },
        { "CO-CAL", (2024, "Caldas") },
        { "CO-CAQ", (1380, "Caqueta") },
        { "CO-CAS", (3299, "Casanare") },
        { "CO-CAU", (3017, "Cauca") },
        { "CO-CES", (1918, "Cesar") },
        { "CO-CHO", (1717, "Choco") },
        { "CO-COR", (2280, "Cordoba") },
        { "CO-CUN", (4010, "Cundinamarca") },
        { "CO-DC", (1004, "Distrito Capital de Bogota") },
        { "CO-GUA", (0, "Guainia") },
        { "CO-GUV", (360, "Guaviare") },
        { "CO-HUI", (2479, "Huila") },
        { "CO-LAG", (2208, "La Guajira") },
        { "CO-MAG", (2354, "Magdalena") },
        { "CO-MET", (4191, "Meta") },
        { "CO-NAR", (3514, "Narino") },
        { "CO-NSA", (2501, "Norte de Santander") },
        { "CO-PUT", (1610, "Putumayo") },
        { "CO-QUI", (1186, "Quindio") },
        { "CO-RIS", (1610, "Risaralda") },
        { "CO-SAN", (3203, "Santander") },
        { "CO-SAP", (0, "San Andrés") },
        { "CO-SUC", (1710, "Sucre") },
        { "CO-TOL", (2673, "Tolima") },
        { "CO-VAC", (3226, "Valle del Cauca") },
        { "CO-VAU", (0, "Vaupes") },
        { "CO-VID", (110, "Vichada") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CR = new()
    {
        { "CR-A", (2, "Provincia de Alajuela") },
        { "CR-C", (2, "Provincia de Cartago") },
        { "CR-G", (4, "Provincia de Guanacaste") },
        { "CR-H", (3, "Provincia de Heredia") },
        { "CR-SJ", (11, "Provincia de San Jose") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CW = new()
    {
        { "NL-CW", (1250, "Curaçao") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CX = new()
    {
        { "Christmas Island", (1, "Christmas Island") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CZ = new()
    {
        { "CZ-10", (784, "Prague") },
        { "CZ-20", (9781, "Stredocesky kraj") },
        { "CZ-31", (7767, "Jihocesky kraj") },
        { "CZ-32", (5360, "Plzensky kraj") },
        { "CZ-41", (2063, "Karlovarsky kraj") },
        { "CZ-42", (4229, "Ustecky kraj") },
        { "CZ-51", (2587, "Liberecky kraj") },
        { "CZ-52", (4052, "Kralovehradecky kraj") },
        { "CZ-53", (3834, "Pardubicky kraj") },
        { "CZ-63", (5756, "Kraj Vysocina") },
        { "CZ-64", (5001, "Jihomoravsky kraj") },
        { "CZ-71", (3831, "Olomoucky kraj") },
        { "CZ-72", (2569, "Zlinsky kraj") },
        { "CZ-80", (4227, "Moravskoslezsky kraj") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> DE = new()
    {
        { "DE-BB", (7660, "Brandenburg") },
        { "DE-BE", (1373, "Land Berlin") },
        { "DE-BW", (10219, "Baden-Wurttemberg") },
        { "DE-BY", (17072, "Bavaria") },
        { "DE-HB", (314, "Bremen") },
        { "DE-HE", (4122, "Hessen") },
        { "DE-HH", (765, "Free and Hanseatic City of Hamburg") },
        { "DE-MV", (5897, "Mecklenburg-Western Pomerania") },
        { "DE-NI", (13443, "Lower Saxony") },
        { "DE-NW", (12916, "Nordrhein-Westfalen") },
        { "DE-RP", (5699, "Rheinland-Pfalz") },
        { "DE-SH", (4580, "Schleswig-Holstein") },
        { "DE-SL", (909, "Saarland") },
        { "DE-SN", (5378, "Saxony") },
        { "DE-ST", (5422, "Saxony-Anhalt") },
        { "DE-TH", (4231, "Thuringia") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> DK = new()
    {
        { "DK-81", (100, "North Denmark Region") },
        { "DK-82", (140, "Region Midtjylland") },
        { "DK-83", (150, "Region Syddanmark") },
        { "DK-84", (60, "Region Hovedstaden") },
        { "DK-85", (130, "Region Sjaelland") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> DO = new()
    {
        { "DO-01", (143, "Distrito Nacional") },
        { "DO-13", (2, "Provincia de La Vega") },
        { "DO-25", (330, "Provincia de Santiago") },
        { "DO-32", (424, "Provincia de Santo Domingo") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> EC = new()
    {
        { "EC-A", (1206, "Azuay") },
        { "EC-B", (870, "Bolivar") },
        { "EC-C", (884, "Carchi") },
        { "EC-D", (1450, "Orellana") },
        { "EC-E", (1440, "Esmeraldas") },
        { "EC-F", (871, "Canar") },
        { "EC-G", (2160, "Guayas") },
        { "EC-H", (1156, "Chimborazo") },
        { "EC-I", (1027, "Imbabura") },
        { "EC-L", (1934, "Loja") },
        { "EC-M", (2478, "Manabi") },
        { "EC-N", (1342, "Napo") },
        { "EC-O", (1488, "El Oro") },
        { "EC-P", (2067, "Pichincha") },
        { "EC-R", (1262, "Los Rios") },
        { "EC-S", (1144, "Morona Santiago") },
        { "EC-SD", (1301, "Santo Domingo de los Tsachilas") },
        { "EC-SE", (932, "Santa Elena") },
        { "EC-T", (1013, "Tungurahua") },
        { "EC-U", (1618, "Sucumbios") },
        { "EC-X", (1139, "Cotopaxi") },
        { "EC-Y", (1486, "Pastaza") },
        { "EC-Z", (256, "Zamora Chinchipe") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> EE = new()
    {
        { "EE-37", (620, "Harjumaa") },
        { "Hiiumaa vald", (120, "Harjumaa") },
        { "EE-39", (80, "Hiiumaa") },
        { "EE-44", (350, "Ida-Virumaa") },
        { "EE-50", (300, "Jogevamaa") },
        { "EE-52", (290, "Jarvamaa") },
        { "EE-57", (265, "Laanemaa") },
        { "EE-59", (450, "Laane-Virumaa") },
        { "EE-64", (200, "Polvamaa") },
        { "EE-68", (570, "Parnumaa") },
        { "EE-70", (370, "Raplamaa") },
        { "EE-74", (360, "Saaremaa") },
        { "EE-79", (290, "Tartumaa") },
        { "EE-81", (220, "Valgamaa") },
        { "EE-84", (290, "Viljandimaa") },
        { "EE-87", (210, "Vorumaa") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> EG = new()
    {
        { "EG-C", (2, "Cairo Governorate") },
        { "EG-GZ", (5, "Muḩafazat al Jizah") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> ES = new()
    {
        { "ES-AN", (11600, "Andalusia") },
        { "ES-AR", (8300, "Aragon") },
        { "ES-AS", (3900, "Principality of Asturias") },
        { "ES-CB", (3500, "Cantabria") },
        { "ES-CE", (400, "Ceuta") },
        { "ES-CL", (12500, "Castilla y Leon") },
        { "ES-CM", (10250, "Castilla-La Mancha") },
        { "ES-CN", (3000, "Provincia de Santa Cruz de Tenerife") },
        { "ES-CT", (7500, "Catalunya") },
        { "ES-EX", (7000, "Extremadura") },
        { "ES-GA", (6800, "Galicia") },
        { "ES-IB", (3000, "Illes Balears") },
        { "ES-MC", (4000, "Region de Murcia") },
        { "ES-MD", (3500, "Comunidad de Madrid") },
        { "ES-ML", (350, "Melilla") },
        { "ES-NC", (4000, "Navarra") },
        { "ES-PV", (3700, "Euskal Autonomia Erkidegoa") },
        { "ES-RI", (2000, "La Rioja") },
        { "ES-VC", (5500, "Comunitat Valenciana") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> FI = new()
    {
        { "FI-02", (359, "South Karelia Region") },
        { "FI-03", (653, "South Ostrobothnia") },
        { "FI-04", (815, "Southern Savonia") },
        { "FI-05", (952, "Kainuu") },
        { "FI-06", (336, "Kanta-Hame") },
        { "FI-07", (302, "Keski-Pohjanmaa") },
        { "FI-08", (926, "Mellersta Finland") },
        { "FI-09", (365, "Kymenlaakso") },
        { "FI-11", (839, "Pirkanmaa") },
        { "FI-12", (758, "Pohjanmaa") },
        { "FI-13", (961, "Pohjois-Karjala") },
        { "FI-14", (1823, "Pohjois-Pohjanmaa") },
        { "FI-15", (947, "Pohjois-Savo") },
        { "FI-16", (383, "Paijat-Hame Region") },
        { "FI-17", (566, "Satakunta") },
        { "FI-18", (1340, "Uusimaa") },
        { "FI-19", (999, "Varsinais-Suomi") },
        { "FI-10", (2200, "Lapland") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> FO = new()
    {
        { "Norðoyar region", (300, "Norðoyar region") },
        { "Streymoy region", (600, "Streymoy region") },
        { "Vágar region", (300, "Vágar region") },
        { "Eysturoy region", (650, "Eysturoy region") },
        { "Sandoy region", (250, "Sandoy region") },
        { "Suðuroy region", (550, "Suðuroy region") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> FR = new()
    {
        { "FR-01", (868, "Ain") },
        { "FR-02", (1024, "Aisne") },
        { "FR-03", (1046, "Allier") },
        { "FR-04", (954, "Alpes-de-Haute-Provence") },
        { "FR-05", (722, "Hautes-Alpes") },
        { "FR-06", (755, "Alpes-Maritimes") },
        { "FR-07", (787, "Ardèche") },
        { "FR-08", (704, "Ardennes") },
        { "FR-09", (632, "Ariège") },
        { "FR-10", (809, "Aube") },
        { "FR-11", (860, "Aude") },
        { "FR-12", (1248, "Aveyron") },
        { "FR-13", (1070, "Bouches-du-Rhône") },
        { "FR-14", (900, "Calvados") },
        { "FR-15", (784, "Cantal") },
        { "FR-16", (815, "Charente") },
        { "FR-17", (999, "Charente-Maritime") },
        { "FR-18", (998, "Cher") },
        { "FR-19", (846, "Corrèze") },
        { "FR-21", (1246, "Côte-d'Or") },
        { "FR-22", (1085, "Côtes-d'Armor") },
        { "FR-23", (727, "Creuse") },
        { "FR-24", (1472, "Dordogne") },
        { "FR-25", (819, "Doubs") },
        { "FR-26", (945, "Drôme") },
        { "FR-27", (909, "Eure") },
        { "FR-28", (851, "Eure-et-Loir") },
        { "FR-29", (1220, "Finistère") },
        { "FR-2A", (543, "Corse-du-Sud") },
        { "FR-2B", (653, "Haute-Corse") },
        { "FR-30", (932, "Gard") },
        { "FR-31", (1128, "Haute-Garonne") },
        { "FR-32", (844, "Gers") },
        { "FR-33", (1757, "Gironde") },
        { "FR-34", (1089, "Hérault") },
        { "FR-35", (1142, "Ille-et-Vilaine") },
        { "FR-36", (898, "Indre") },
        { "FR-37", (949, "Indre-et-Loire") },
        { "FR-38", (1258, "Isère") },
        { "FR-39", (784, "Jura") },
        { "FR-40", (1244, "Landes") },
        { "FR-41", (894, "Loir-et-Cher") },
        { "FR-42", (774, "Loire") },
        { "FR-43", (679, "Haute-Loire") },
        { "FR-44", (1253, "Loire-Atlantique") },
        { "FR-45", (1086, "Loiret") },
        { "FR-46", (735, "Lot") },
        { "FR-47", (771, "Lot-et-Garonne") },
        { "FR-48", (677, "Lozère") },
        { "FR-49", (1108, "Maine-et-Loire") },
        { "FR-50", (996, "Manche") },
        { "FR-51", (1108, "Marne") },
        { "FR-52", (854, "Haute-Marne") },
        { "FR-53", (700, "Mayenne") },
        { "FR-54", (799, "Meurthe-et-Moselle") },
        { "FR-55", (794, "Meuse") },
        { "FR-56", (1111, "Morbihan") },
        { "FR-57", (1014, "Moselle") },
        { "FR-58", (915, "Nièvre") },
        { "FR-59", (1387, "Nord") },
        { "FR-60", (901, "Oise") },
        { "FR-61", (813, "Orne") },
        { "FR-62", (1189, "Pas-de-Calais") },
        { "FR-63", (1204, "Puy-de-Dôme") },
        { "FR-64", (1121, "Pyrénées-Atlantiques") },
        { "FR-65", (621, "Hautes-Pyrénées") },
        { "FR-66", (664, "Pyrénées-Orientales") },
        { "FR-69", (682, "Rhône") },
        { "FR-69M", (188, "Métropole de Lyon") },
        { "FR-6AE", (1494, "Alsace") },
        { "FR-70", (724, "Haute-Saône") },
        { "FR-71", (1207, "Saône-et-Loire") },
        { "FR-72", (1035, "Sarthe") },
        { "FR-73", (875, "Savoie") },
        { "FR-74", (752, "Haute-Savoie") },
        { "FR-75C", (370, "Paris") },
        { "FR-76", (1132, "Seine-Maritime") },
        { "FR-77", (1068, "Seine-et-Marne") },
        { "FR-78", (570, "Yvelines") },
        { "FR-79", (850, "Deux-Sèvres") },
        { "FR-80", (909, "Somme") },
        { "FR-81", (799, "Tarn") },
        { "FR-82", (534, "Tarn-et-Garonne") },
        { "FR-83", (982, "Var") },
        { "FR-84", (643, "Vaucluse") },
        { "FR-85", (990, "Vendée") },
        { "FR-86", (980, "Vienne") },
        { "FR-87", (802, "Haute-Vienne") },
        { "FR-88", (823, "Vosges") },
        { "FR-89", (1075, "Yonne") },
        { "FR-90", (105, "Territoire de Belfort") },
        { "FR-91", (522, "Essonne") },
        { "FR-92", (295, "Hauts-de-Seine") },
        { "FR-93", (312, "Seine-Saint-Denis") },
        { "FR-94", (276, "Val-de-Marne") },
        { "FR-95", (403, "Val-d'Oise") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GB = new()
    {
        { "GB-ENG", (10380, "England") },
        { "GB-NIR", (1551, "Northern Ireland") },
        { "GB-SCT", (3577, "Scotland") },
        { "GB-WLS", (1906, "Wales") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GH = new()
    {
        { "GH-AA", (908, "Greater Accra") },
        { "GH-AF", (120, "Ahafo") },
        { "GH-AH", (1218, "Ashanti") },
        { "GH-BE", (75, "Bono East") },
        { "GH-BO", (129, "Bono") },
        { "GH-CP", (661, "Central") },
        { "GH-EP", (638, "Eastern") },
        { "GH-NE", (33, "North East") },
        { "GH-NP", (291, "Northern") },
        { "GH-OT", (153, "Oti") },
        { "GH-SV", (416, "Savannah") },
        { "GH-TV", (379, "Volta") },
        { "GH-UE", (64, "Upper East") },
        { "GH-UW", (528, "Upper West") },
        { "GH-WN", (14, "Western North") },
        { "GH-WP", (561, "Western") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GI = new()
    {
        { "Gibraltar", (100, "Gibraltar") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GL = new()
    {
        { "GL-AV", (30, "Avannaata") },
        { "GL-KU", (38, "Kujalleq") },
        { "GL-QE", (40, "Qeqqata") },
        { "GL-QT", (19, "Qeqertalik") },
        { "GL-SM", (53, "Sermersooq") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GR = new()
    {
        { "GR-A", (1263, "Eastern Macedonia and Thrace") },
        { "GR-B", (1675, "Central Macedonia") },
        { "GR-C", (846, "Western Macedonia") },
        { "GR-D", (852, "Epirus") },
        { "GR-E", (1281, "Thessaly") },
        { "GR-F", (540, "Ionian Islands") },
        { "GR-G", (1115, "Western Greece") },
        { "GR-H", (1397, "Central Greece") },
        { "GR-I", (946, "Attica") },
        { "GR-J", (1476, "Peloponnese") },
        { "GR-K", (354, "North Aegean") },
        { "GR-L", (486, "South Aegean") },
        { "GR-M", (858, "Crete") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GT = new()
    {
        { "GT-01", (617, "Departamento de Guatemala") },
        { "GT-02", (147, "Departamento de El Progreso") },
        { "GT-03", (154, "Departamento de Sacatepequez") },
        { "GT-04", (133, "Departamento de Chimaltenango") },
        { "GT-05", (402, "Departamento de Escuintla") },
        { "GT-06", (277, "Departamento de Santa Rosa") },
        { "GT-07", (218, "Departamento de Solola") },
        { "GT-08", (104, "Departamento de Totonicapan") },
        { "GT-09", (256, "Departamento de Quetzaltenango") },
        { "GT-10", (148, "Departamento de Suchitepequez") },
        { "GT-11", (133, "Departamento de Retalhuleu") },
        { "GT-12", (294, "Departamento de San Marcos") },
        { "GT-13", (219, "Departamento de Huehuetenango") },
        { "GT-14", (263, "Departamento del Quiche") },
        { "GT-15", (170, "Departamento de Baja Verapaz") },
        { "GT-16", (329, "Departamento de Alta Verapaz") },
        { "GT-17", (401, "Departamento del Peten") },
        { "GT-18", (268, "Departamento de Izabal") },
        { "GT-19", (95, "Departamento de Zacapa") },
        { "GT-20", (178, "Departamento de Chiquimula") },
        { "GT-21", (202, "Departamento de Jalapa") },
        { "GT-22", (235, "Departamento de Jutiapa") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GU = new()
    {
        { "US-GU", (1, "Guam") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> HK = new()
    {
        { "CN-HK", (1, "Hong Kong") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> HR = new()
    {
        { "HR-01", (510, "Zagreb County") },
        { "HR-02", (210, "Krapina-Zagorje") },
        { "HR-03", (510, "Sisak-Moslavina") },
        { "HR-04", (410, "Karlovac") },
        { "HR-05", (220, "Varazdin") },
        { "HR-06", (230, "Koprivinica-Krizevci") },
        { "HR-07", (400, "Bjelovar-Bilogora") },
        { "HR-08", (700, "Primorsko-Goranska Zupanija") },
        { "HR-09", (650, "Lika-Senj") },
        { "HR-10", (250, "Virovitica-Podravina") },
        { "HR-11", (240, "Pozega-Slavonia") },
        { "HR-12", (250, "Brod-Posavina") },
        { "HR-13", (650, "Zadar") },
        { "HR-14", (470, "Osjecko-Baranjska Zupanija") },
        { "HR-15", (520, "Sibenik-Knin") },
        { "HR-16", (300, "Vukovar-Srijem") },
        { "HR-17", (800, "Split-Dalmatia") },
        { "HR-18", (600, "Istria") },
        { "HR-19", (450, "Dubrovacko-Neretvanska Zupanija") },
        { "HR-20", (150, "Medimurje") },
        { "HR-21", (130, "Grad Zagreb") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> HU = new()
    {
        { "HU-BA", (911, "Baranya") },
        { "HU-BE", (1045, "Bekes") },
        { "HU-BK", (1615, "Bacs-Kiskun") },
        { "HU-BU", (1060, "Budapest") },
        { "HU-BZ", (1475, "Borsod-Abauj-Zemplen") },
        { "HU-CS", (856, "Csongrad-Csanad") },
        { "HU-FE", (879, "Fejer") },
        { "HU-GS", (1311, "Gyor-Moson-Sopron") },
        { "HU-HB", (1203, "Hajdu-Bihar") },
        { "HU-HE", (733, "Heves") },
        { "HU-JN", (1018, "Jasz-Nagykun-Szolnok") },
        { "HU-KE", (491, "Komarom-Esztergom") },
        { "HU-NO", (507, "Nograd") },
        { "HU-PE", (1542, "Pest") },
        { "HU-SO", (1126, "Somogy") },
        { "HU-SZ", (1185, "Szabolcs-Szatmar-Bereg") },
        { "HU-TO", (713, "Tolna") },
        { "HU-VA", (667, "Vas") },
        { "HU-VE", (893, "Veszprem") },
        { "HU-ZA", (770, "Zala") },
        { "Great Plain and North", (0, "Great Plain and North") },
        { "Central Hungary", (0, "Central Hungary") },
        { "Transdanubia", (0, "Transdanubia") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> ID = new()
    {
        { "ID-AC", (800, "Aceh") },
        { "ID-BA", (4000, "Bali") },
        { "ID-BB", (2300, "Kepulauan Bangka Belitung") },
        { "ID-BE", (2000, "Bengkulu") },
        { "ID-BT", (2300, "Banten") },
        { "ID-GO", (2000, "Gorontalo") },
        { "ID-JA", (2500, "Jambi") },
        { "ID-JB", (6200, "Jawa Barat") },
        { "ID-JI", (8400, "Jawa Timur") },
        { "ID-JK", (600, "Jakarta Raya") },
        { "ID-JT", (6200, "Jawa Tengah") },
        { "ID-KB", (4200, "Kalimantan Barat") },
        { "ID-KI", (3600, "Kalimantan Timur") },
        { "ID-KR", (1500, "Kepulauan Riau") },
        { "ID-KS", (3400, "Kalimantan Selatan") },
        { "ID-KT", (3000, "Kalimantan Tengah") },
        { "ID-KU", (1200, "Kalimantan Utara") },
        { "ID-LA", (4000, "Lampung") },
        { "ID-MU", (1400, "Maluku Utara") },
        { "ID-NB", (5400, "Nusa Tenggara Barat") },
        { "ID-NT", (7200, "Nusa Tenggara Timur") },
        { "ID-RI", (4100, "Riau") },
        { "ID-SA", (3300, "Sulawesi Utara") },
        { "ID-SB", (3900, "Sumatera Barat") },
        { "ID-SG", (3400, "Sulawesi Tenggara") },
        { "ID-SN", (5700, "Sulawesi Selatan") },
        { "ID-SR", (1400, "Sulawesi Barat") },
        { "ID-SS", (4100, "Sumatera Selatan") },
        { "ID-ST", (4700, "Sulawesi Tengah") },
        { "ID-SU", (6400, "Sumatera Utara") },
        { "ID-YO", (1000, "Yogyakarta") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> IE = new()
    {
        { "IE-CE", (3207, "Clare") },
        { "IE-CN", (2336, "Cavan") },
        { "IE-CO", (8777, "Cork") },
        { "IE-CW", (1006, "Carlow") },
        { "IE-D", (1234, "Dublin") },
        { "IE-DL", (4724, "Donegal") },
        { "IE-G", (5765, "Galway") },
        { "IE-KE", (1832, "Kildare") },
        { "IE-KK", (2324, "Kilkenny") },
        { "IE-KY", (4037, "Kerry") },
        { "IE-LD", (1179, "Longford") },
        { "IE-LH", (1037, "Louth") },
        { "IE-LK", (2888, "Limerick") },
        { "IE-LM", (1663, "Leitrim") },
        { "IE-LS", (1811, "Laois") },
        { "IE-MH", (2724, "Meath") },
        { "IE-MN", (1783, "Monaghan") },
        { "IE-MO", (4954, "Mayo") },
        { "IE-OY", (1758, "Offaly") },
        { "IE-RN", (2924, "Roscommon") },
        { "IE-SO", (1941, "Sligo") },
        { "IE-TA", (4414, "Tipperary") },
        { "IE-WD", (2043, "Waterford") },
        { "IE-WH", (1816, "Westmeath") },
        { "IE-WW", (1718, "Wicklow") },
        { "IE-WX", (2830, "Wexford") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> IL = new()
    {
        { "IL-D", (651, "Southern District") },
        { "IL-HA", (361, "Haifa") },
        { "IL-JM", (149, "Jerusalem") },
        { "IL-M", (596, "Central District") },
        { "IL-TA", (209, "Tel Aviv District") },
        { "IL-Z", (610, "Northern District") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> IM = new()
    {
        { "IM-1", (562, "Isle of Man") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> IN = new()
    {
        { "IN-AP", (2660, "Andhra Pradesh") },
        { "IN-AR", (192, "Arunachal Pradesh") },
        { "IN-AS", (1143, "Assam") },
        { "IN-BR", (1908, "Bihar") },
        { "IN-CH", (4, "Chandigarh") },
        { "IN-CT", (1234, "Chhattisgarh") },
        { "IN-DL", (38, "Delhi") },
        { "IN-GA", (81, "Goa") },
        { "IN-GJ", (3253, "Gujarat") },
        { "IN-HP", (97, "Himachal Pradesh") },
        { "IN-HR", (937, "Haryana") },
        { "IN-JH", (1244, "Jharkhand") },
        { "IN-KA", (3817, "Karnataka") },
        { "IN-KL", (777, "Kerala") },
        { "IN-LA", (17, "Ladakh") },
        { "IN-LD", (4, " Lakshadweep") },
        { "IN-MH", (5910, "Maharashtra") },
        { "IN-ML", (218, "Meghalaya") },
        { "IN-MN", (30, "Manipur") },
        { "IN-MP", (4981, "Madhya Pradesh") },
        { "IN-MZ", (96, "Mizoram") },
        { "IN-NL", (4, "Nāgāland") },
        { "IN-OR", (1746, "Odisha") },
        { "IN-PB", (900, "Punjab") },
        { "IN-PY", (26, "Puducherry") },
        { "IN-RJ", (5914, "Rajasthan") },
        { "IN-SK", (29, "Sikkim") },
        { "IN-TG", (1777, "Telangana") },
        { "IN-TN", (2568, "Tamil Nadu") },
        { "IN-TR", (122, "Tripura") },
        { "IN-UP", (4549, "Uttar Pradesh") },
        { "IN-UT", (486, "Uttarakhand") },
        { "IN-WB", (1783, "West Bengal") },
        { "IN-DH", (26, "Dadra and Nagar Haveli and Daman and Diu") },
        { "IN-JK", (52, "Jammu and Kashmir") },
        { "IN-AN", (33, "Andaman and Nicobar Islands") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> IS = new()
    {
        { "IS-1", (87, "Hofudborgarsvaedi") },
        { "IS-2", (51, "Sudurnes") },
        { "IS-3", (370, "Vesturland") },
        { "IS-4", (313, "Vestfirdir") },
        { "IS-5", (324, "Nordurland Vestra") },
        { "IS-6", (369, "Nordurland Eystra") },
        { "IS-7", (410, "Austurland") },
        { "IS-8", (531, "Sudurland") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> IT = new()
    {
        { "IT-21", (25387, "Piemonte") },
        { "IT-23", (3261, "Regione Autonoma Valle d'Aosta") },
        { "IT-25", (23864, "Lombardia") },
        { "IT-32", (13606, "Trentino-Alto Adige") },
        { "IT-34", (18345, "Veneto") },
        { "IT-36", (7924, "Friuli Venezia Giulia") },
        { "IT-42", (5416, "Liguria") },
        { "IT-45", (22453, "Emilia-Romagna") },
        { "IT-52", (22987, "Toscana") },
        { "IT-55", (8464, "Umbria") },
        { "IT-57", (9401, "Marche") },
        { "IT-62", (17232, "Lazio") },
        { "IT-65", (10832, "Abruzzo") },
        { "IT-67", (4461, "Molise") },
        { "IT-72", (13671, "Campania") },
        { "IT-75", (19541, "Puglia") },
        { "IT-77", (10073, "Basilicata") },
        { "IT-78", (15222, "Calabria") },
        { "IT-82", (25832, "Sicilia") },
        { "IT-88", (24100, "Sardegna") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> JE = new()
    {
        { "JE-1", (196, "Jersey") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> JO = new()
    {
        { "JO-AM", (382, "Al 'Asimah") },
        { "JO-AQ", (213, "Al 'Aqabah") },
        { "JO-AT", (78, "At Tafilah") },
        { "JO-AZ", (1, "Az Zarqa'") },
        { "JO-BA", (40, "Al Balqa'") },
        { "JO-JA", (9, "Jarash") },
        { "JO-KA", (130, "Al Karak") },
        { "JO-MD", (45, "Madaba") },
        { "JO-MN", (180, "Ma'an") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> JP = new()
    {
        { "JP-01", (22854, "Hokkaido") },
        { "JP-02", (6261, "Aomori") },
        { "JP-03", (9807, "Iwate") },
        { "JP-04", (6966, "Miyagi") },
        { "JP-05", (6846, "Akita") },
        { "JP-06", (5340, "Yamagata") },
        { "JP-07", (10596, "Fukushima") },
        { "JP-08", (8141, "Ibaraki") },
        { "JP-09", (6385, "Tochigi") },
        { "JP-10", (5129, "Gunma") },
        { "JP-11", (5204, "Saitama") },
        { "JP-12", (7393, "Chiba") },
        { "JP-13", (2753, "Tokyo") },
        { "JP-14", (3135, "Kanagawa") },
        { "JP-15", (9192, "Niigata") },
        { "JP-16", (3255, "Toyama") },
        { "JP-17", (3881, "Ishikawa") },
        { "JP-18", (3049, "Fukui") },
        { "JP-19", (2951, "Yamanashi") },
        { "JP-20", (8670, "Nagano") },
        { "JP-21", (6541, "Gifu") },
        { "JP-22", (6960, "Shizuoka") },
        { "JP-23", (6830, "Aichi") },
        { "JP-24", (5235, "Mie") },
        { "JP-25", (3080, "Shiga") },
        { "JP-26", (3759, "Kyoto") },
        { "JP-27", (2706, "Osaka") },
        { "JP-28", (7693, "Hyogo") },
        { "JP-29", (2658, "Nara") },
        { "JP-30", (3695, "Wakayama") },
        { "JP-31", (2983, "Tottori") },
        { "JP-32", (5532, "Shimane") },
        { "JP-33", (7293, "Okayama") },
        { "JP-34", (8065, "Hiroshima") },
        { "JP-35", (5833, "Yamaguchi") },
        { "JP-36", (3226, "Tokushima") },
        { "JP-37", (2383, "Kagawa") },
        { "JP-38", (4884, "Ehime") },
        { "JP-39", (4353, "Kochi") },
        { "JP-40", (6178, "Fukuoka") },
        { "JP-41", (3207, "Saga") },
        { "JP-42", (4668, "Nagasaki Prefecture") },
        { "JP-43", (7029, "Kumamoto") },
        { "JP-44", (5975, "Oita") },
        { "JP-45", (5668, "Miyazaki") },
        { "JP-46", (9724, "Kagoshima") },
        { "JP-47", (2525, "Okinawa") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> KE = new()
    {
        { "KE-01", (138, "Baringo") },
        { "KE-02", (141, "Bomet") },
        { "KE-03", (220, "Bungoma") },
        { "KE-04", (191, "Busia") },
        { "KE-05", (112, "Elgeyo/Marakwet") },
        { "KE-06", (119, "Embu") },
        { "KE-07", (141, "Garissa") },
        { "KE-08", (155, "Homa Bay") },
        { "KE-09", (103, "Isiolo") },
        { "KE-10", (421, "Kajiado") },
        { "KE-11", (220, "Kakamega") },
        { "KE-12", (128, "Kericho") },
        { "KE-13", (279, "Kiambu") },
        { "KE-14", (168, "Kilifi") },
        { "KE-15", (105, "Kirinyaga") },
        { "KE-16", (155, "Kisii") },
        { "KE-17", (134, "Kisumu") },
        { "KE-18", (424, "Kitui") },
        { "KE-19", (146, "Kwale") },
        { "KE-20", (315, "Laikipia") },
        { "KE-22", (338, "Machakos") },
        { "KE-23", (403, "Makueni") },
        { "KE-25", (106, "Marsabit") },
        { "KE-26", (400, "Meru") },
        { "KE-27", (167, "Migori") },
        { "KE-28", (41, "Mombasa") },
        { "KE-29", (261, "Murang'a") },
        { "KE-30", (110, "Nairobi City") },
        { "KE-31", (369, "Nakuru") },
        { "KE-32", (124, "Nandi") },
        { "KE-33", (482, "Narok") },
        { "KE-34", (102, "Nyamira") },
        { "KE-35", (161, "Nyandarua") },
        { "KE-36", (231, "Nyeri") },
        { "KE-37", (98, "Samburu") },
        { "KE-38", (219, "Siaya") },
        { "KE-39", (354, "Taita/Taveta") },
        { "KE-40", (82, "Tana River") },
        { "KE-41", (76, "Tharaka-Nithi") },
        { "KE-42", (174, "Trans Nzoia") },
        { "KE-43", (249, "Turkana") },
        { "KE-44", (262, "Uasin Gishu") },
        { "KE-45", (66, "Vihiga") },
        { "KE-46", (164, "Wajir") },
        { "KE-47", (102, "West Pokot") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> KG = new()
    {
        { "KG-C", (590, "Chuyskaya Oblast’") },
        { "KG-GB", (213, "Gorod Bishkek") },
        { "KG-GO", (47, "Osh") },
        { "KG-J", (443, "Jalal-Abad oblast") },
        { "KG-N", (359, "Naryn oblast") },
        { "KG-O", (97, "Osh Oblasty") },
        { "KG-T", (126, "Talas") },
        { "KG-Y", (441, "Issyk-Kul Region") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> KH = new()
    {
        { "KH-1", (1856, "Banteay Mean Choay") },
        { "KH-10", (512, "Kracheh") },
        { "KH-11", (473, "Mondol Kiri") },
        { "KH-12", (928, "Phnom Penh") },
        { "KH-13", (813, "Preah Vihear") },
        { "KH-14", (2626, "Prey Veaeng") },
        { "KH-15", (1176, "Pousaat") },
        { "KH-16", (554, "Rotanak Kiri") },
        { "KH-17", (3359, "Siem Reab") },
        { "KH-18", (710, "Preah Sihanouk") },
        { "KH-19", (523, "Stueng Traeng") },
        { "KH-2", (3426, "Baat Dambang") },
        { "KH-20", (1912, "Svaay Rieng") },
        { "KH-21", (2301, "Taakaev") },
        { "KH-22", (441, "Otdar Mean Chey") },
        { "KH-23", (164, "Kaeb") },
        { "KH-24", (417, "Pailin") },
        { "KH-25", (2042, "Tbong Khmum") },
        { "KH-3", (1796, "Kampong Chaam") },
        { "KH-4", (968, "Kampong Chhnang") },
        { "KH-5", (3056, "Kampong Spueu") },
        { "KH-6", (854, "Kampong Thum") },
        { "KH-7", (2485, "Kampot") },
        { "KH-8", (2121, "Kandaal") },
        { "KH-9", (857, "Kaoh Kong") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> KR = new()
    {
        { "KR-11", (891, "Seoul-teukbyeolsi") },
        { "KR-26", (836, "Busan-gwangyeoksi") },
        { "KR-27", (791, "Daegu-gwangyeoksi") },
        { "KR-28", (1071, "Incheon-gwangyeoksi") },
        { "KR-29", (37, "Gwangju-gwangyeoksi") },
        { "KR-30", (534, "Daejeon-gwangyeoksi") },
        { "KR-31", (91, "Ulsan-gwangyeoksi") },
        { "KR-41", (5919, "Gyeonggi-do") },
        { "KR-42", (3599, "Gangwon-do") },
        { "KR-43", (123, "Chungcheongbuk-do") },
        { "KR-44", (415, "Chungcheongnam-do") },
        { "KR-45", (1536, "Jeollabuk-do") },
        { "KR-46", (2782, "Jeollanam-do") },
        { "KR-47", (5391, "Gyeongsangbuk-do") },
        { "KR-48", (1857, "Gyeongsangnam-do") },
        { "KR-49", (1589, "Jeju-teukbyeoljachido") },
        { "KR-50", (4, "Sejong") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> KZ = new()
    {
        { "KZ-10", (561, "Abai") },
        { "KZ-75", (517, "Almaty") },
        { "KZ-19", (677, "Almaty Oblysy") },
        { "KZ-11", (1416, "Aqmola Oblysy") },
        { "KZ-15", (1355, "Aktyubinskaya Oblast’") },
        { "KZ-71", (326, "Astana") },
        { "KZ-23", (559, "Atyrau Oblysy") },
        { "KZ-27", (534, "West Kazakhstan") },
        { "KZ-47", (837, "Mangistauskaya Oblast’") },
        { "KZ-55", (1149, "Pavlodar Region") },
        { "KZ-35", (1316, "Qaraghandy Oblysy") },
        { "KZ-39", (744, "Qostanay Oblysy") },
        { "KZ-43", (551, "Qyzylorda Oblysy") },
        { "KZ-63", (610, "East Kazakhstan") },
        { "KZ-79", (612, "Shymkent") },
        { "KZ-59", (676, "North Kazakhstan") },
        { "KZ-61", (733, "Turkistan") },
        { "KZ-62", (263, "Ulytau Audany") },
        { "KZ-31", (697, "Zhambyl Oblysy") },
        { "KZ-33", (795, "Zhetysu") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> LA = new()
    {
        { "LA-CH", (20, "Champasak") },
        { "LA-LP", (12, "Luang Prabang Province") },
        { "LA-SV", (26, "Khoueng Savannakhet") },
        { "LA-VI", (4, "Vientiane Province") },
        { "LA-VT", (210, "Vientiane Prefecture") },
        { "LA-XA", (0, "Xaignabouli") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> LB = new()
    {
        { "LB-BH", (1, "Mohafazat Baalbek-Hermel") },
        { "LB-BI", (19, "Mohafazat Beqaa") },
        { "LB-BA", (13, "Beyrouth") },
        { "LB-AS", (5, "Mohafazat Liban-Nord") },
        { "LB-JA", (1, "Mohafazat Liban-Sud") },
        { "LB-JL", (28, "Mohafazat Mont-Liban") },
        { "LB-NA", (1, "Mohafazat Nabatiye") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> LI = new()
    {
        { "LI-01", (1, "Balzers") },
        { "LI-02", (3, "Eschen") },
        { "LI-03", (3, "Gamprin") },
        { "LI-04", (2, "Mauren") },
        { "LI-05", (1, "Planken") },
        { "LI-06", (2, "Ruggell") },
        { "LI-07", (5, "Schaan") },
        { "LI-08", (2, "Schellenberg") },
        { "LI-09", (2, "Triesen") },
        { "LI-10", (3, "Triesenberg") },
        { "LI-11", (4, "Vaduz") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> LK = new()
    {
        { "LK-1", (4292, "Western Province") },
        { "LK-2", (3291, "Central Province") },
        { "LK-3", (4075, "Southern Province") },
        { "LK-4", (3422, "Northern Province") },
        { "LK-5", (2983, "Eastern Province") },
        { "LK-6", (4753, "North Western Province") },
        { "LK-7", (5451, "North Central Province") },
        { "LK-8", (2816, "Uva Province") },
        { "LK-9", (2611, "Sabaragamuwa Province") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> LS = new()
    {
        { "LS-A", (597, "Maseru") },
        { "LS-B", (123, "Botha-Bothe") },
        { "LS-C", (289, "Leribe") },
        { "LS-D", (245, "Berea") },
        { "LS-E", (264, "Mafeteng") },
        { "LS-F", (127, "Mohale's Hoek") },
        { "LS-G", (112, "Quthing") },
        { "LS-H", (102, "Qacha's Nek") },
        { "LS-J", (125, "Mokhotlong") },
        { "LS-K", (173, "Thaba-Tseka") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> LT = new()
    {
        { "LT-AL", (786, "Alytus County") },
        { "LT-KL", (848, "Klaipeda County") },
        { "LT-KU", (1351, "Kaunas County") },
        { "LT-MR", (661, "Marijampole County") },
        { "LT-PN", (1144, "Panevezys") },
        { "LT-SA", (1265, "Siauliai County") },
        { "LT-TA", (629, "Taurage County") },
        { "LT-TE", (645, "Telsių apskritis") },
        { "LT-UT", (1020, "Utena County") },
        { "LT-VL", (1651, "Vilnius County") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> LU = new()
    {
        { "LU-CA", (1000, "Capellen") },
        { "LU-CL", (1300, "Clervaux") },
        { "LU-DI", (1000, "Diekirch") },
        { "LU-EC", (700, "Canton d'Echternach") },
        { "LU-ES", (1300, "Canton d'Esch-sur-Alzette") },
        { "LU-GR", (1000, "Grevenmacher") },
        { "LU-LU", (1200, "Luxembourg") },
        { "LU-ME", (900, "Mersch") },
        { "LU-RD", (1000, "Redange") },
        { "LU-RM", (300, "Remich") },
        { "LU-VD", (250, "Vianden") },
        { "LU-WI", (1000, "Wiltz") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> LV = new()
    {
        { "LV-002", (1396, "Aizkraukles novads") },
        { "LV-007", (1061, "Aluksnes Novads") },
        { "LV-011", (243, "Adazu Novads") },
        { "LV-015", (1214, "Balvu Novads") },
        { "LV-016", (1405, "Bauskas Novads") },
        { "LV-022", (1852, "Cesu Novads") },
        { "LV-026", (1019, "Dobeles novads") },
        { "LV-033", (1164, "Gulbenes novads") },
        { "LV-041", (1035, "Jelgavas novads") },
        { "LV-042", (1830, "Jekabpils Municipality") },
        { "LV-047", (1392, "Kraslavas novads") },
        { "LV-050", (1581, "Kuldigas novads") },
        { "LV-052", (407, "Kekavas Novads") },
        { "LV-054", (1498, "Limbazu novads") },
        { "LV-056", (388, "Livanu Novads") },
        { "LV-058", (1466, "Ludzas novads") },
        { "LV-059", (1890, "Madona Municipality") },
        { "LV-062", (190, "Marupes Novads") },
        { "LV-067", (1296, "Ogres novads") },
        { "LV-068", (256, "Olaines Novads") },
        { "LV-073", (855, "Preili Municipality") },
        { "LV-077", (1686, "Rezeknes Novads") },
        { "LV-080", (274, "Ropazu Novads") },
        { "LV-087", (171, "Salaspils Novads") },
        { "LV-088", (1321, "Saldus Municipality") },
        { "LV-089", (206, "Saulkrastu Novads") },
        { "LV-091", (728, "Siguldas Novads") },
        { "LV-094", (1111, "Smiltenes Novads") },
        { "LV-097", (1738, "Talsi Municipality") },
        { "LV-099", (1207, "Tukuma novads") },
        { "LV-101", (541, "Valka Municipality") },
        { "LV-102", (65, "Varaklanu Novads") },
        { "LV-106", (1150, "Ventspils Municipality") },
        { "LV-111", (1523, "Augsdaugava Municipality") },
        { "LV-112", (2148, "South Kurzeme Municipality") },
        { "LV-113", (1872, "Valmiera") },
        { "LV-DGV", (293, "Daugavpils") },
        { "LV-JEL", (204, "Jelgava") },
        { "LV-JUR", (224, "Jurmala") },
        { "LV-LPX", (209, "Liepaja") },
        { "LV-REZ", (93, "Rezekne") },
        { "LV-RIX", (2102, "Riga") },
        { "LV-VEN", (133, "Ventspils") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MC = new()
    {
        { "MC-1", (100, "N/A") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> ME = new()
    {
        { "ME-01", (470, "Andrijevica") },
        { "ME-02", (1204, "Bar") },
        { "ME-03", (862, "Berane") },
        { "ME-04", (1506, "Bijelo Polje") },
        { "ME-05", (340, "Budva") },
        { "ME-06", (1448, "Cetinje") },
        { "ME-07", (745, "Danilovgrad") },
        { "ME-08", (570, "Herceg Novi") },
        { "ME-09", (1338, "Opstina Kolasin") },
        { "ME-10", (737, "Kotor") },
        { "ME-11", (535, "Mojkovac") },
        { "ME-12", (3337, "Opstina Niksic") },
        { "ME-13", (617, "Opstina Plav") },
        { "ME-14", (2135, "Pljevlja") },
        { "ME-15", (1268, "Opstina Pluzine") },
        { "ME-16", (3313, "Podgorica") },
        { "ME-17", (736, "Opstina Rozaje") },
        { "ME-18", (816, "Opstina Savnik") },
        { "ME-19", (171, "Tivat") },
        { "ME-20", (541, "Ulcinj") },
        { "ME-21", (665, "Opstina Zabljak") },
        { "ME-22", (244, "Gusinje") },
        { "ME-23", (288, "Petnjica") },
        { "ME-24", (116, "Tuzi") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MG = new()
    {
        { "MG-U", (15, "Toliara Province") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MK = new()
    {
        { "MK-101", (982, "Veles") },
        { "MK-102", (428, "Gradsko") },
        { "MK-103", (522, "Demir Kapija") },
        { "MK-104", (1815, "Kavadarci") },
        { "MK-105", (275, "Lozovo") },
        { "MK-106", (765, "Negotino") },
        { "MK-107", (230, "Rosoman") },
        { "MK-108", (822, "Sveti Nikole") },
        { "MK-201", (968, "Berovo") },
        { "MK-202", (783, "Vinica") },
        { "MK-203", (755, "Opstina Delcevo") },
        { "MK-205", (382, "Karbinci") },
        { "MK-206", (658, "Opstina Kocani") },
        { "MK-207", (341, "Makedonska Kamenica") },
        { "MK-208", (358, "Opstina Pehcevo") },
        { "MK-209", (551, "Opstina Probistip") },
        { "MK-210", (229, "Cesinovo-Oblesevo") },
        { "MK-211", (1076, "Opstina Stip") },
        { "MK-304", (716, "Debarca") },
        { "MK-307", (1498, "Opstina Kicevo") },
        { "MK-310", (961, "Ohrid") },
        { "MK-402", (37, "Bosilovo") },
        { "MK-403", (5098, "Valandovo") },
        { "MK-404", (41, "Vasilevo") },
        { "MK-405", (570, "Gevgelija") },
        { "MK-406", (241, "Opstina Dojran") },
        { "MK-408", (675, "Novo Selo") },
        { "MK-409", (830, "Opstina Radovis") },
        { "MK-410", (211, "Strumica") },
        { "MK-501", (1710, "Bitola") },
        { "MK-506", (417, "Mogila") },
        { "MK-508", (2200, "Prilep") },
        { "MK-509", (64, "Resen") },
        { "MK-601", (64, "Bogovinje") },
        { "MK-602", (281, "Brvenica") },
        { "MK-603", (362, "Opstina Vrapciste") },
        { "MK-604", (944, "Gostivar") },
        { "MK-605", (369, "Opstina Zelino") },
        { "MK-607", (1064, "Opstina Mavrovo i Rostusa") },
        { "MK-609", (666, "Tetovo") },
        { "MK-701", (673, "Kratovo") },
        { "MK-702", (803, "Kriva Palanka") },
        { "MK-703", (1370, "Kumanovo") },
        { "MK-704", (457, "Opstina Lipkovo") },
        { "MK-705", (388, "Opstina Rankovce") },
        { "MK-706", (697, "Opstina Staro Nagoricane") },
        { "MK-802", (95, "Opstina Aracinovo") },
        { "MK-807", (232, "Ilinden") },
        { "MK-810", (386, "Petrovec") },
        { "MK-816", (391, "Opstina Cucer-Sandevo") },
        { "MK-85", (3818, "Cair") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> ML = new()
    {
        { "ML-10", (200, "Tombouctou Region") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MN = new()
    {
        { "MN-035", (76, "Orhon Aymag") },
        { "MN-037", (88, "Darhan-Uul Aymag") },
        { "MN-039", (294, "Hentiy Aymag") },
        { "MN-041", (468, "Hovsgol Aymag") },
        { "MN-043", (345, "Hovd") },
        { "MN-046", (194, "Uvs Aymag") },
        { "MN-047", (624, "Central Aymag") },
        { "MN-049", (526, "Selenge Aymag") },
        { "MN-051", (442, "Suhbaatar Aymag") },
        { "MN-053", (146, "Omnogovi Province") },
        { "MN-055", (108, "South Khangay") },
        { "MN-057", (280, "Dzavhan Aymag") },
        { "MN-059", (418, "Dundgovĭ Aymag") },
        { "MN-061", (601, "Dornod Aymag") },
        { "MN-063", (371, "East Gobi Aymag") },
        { "MN-064", (49, "Govĭ-Sumber") },
        { "MN-065", (43, "Govĭ-Altay Aymag") },
        { "MN-067", (114, "Bulgan") },
        { "MN-069", (13, "Bayanhongor Aymag") },
        { "MN-071", (233, "Bayan-Olgiy Aymag") },
        { "MN-073", (301, "Arhangay Aymag") },
        { "MN-1", (450, "Ulan Bator") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MO = new()
    {
        { "CN-MO", (57, "Macao") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MP = new()
    {
        { "US-MP", (1, "Northern Mariana Islands") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MQ = new()
    {
        { "FR-972", (5, "Martinique") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MT = new()
    {
        { "MT-01", (1519, "Attard") },
        { "MT-02", (83, "Balzan") },
        { "MT-03", (58, "Birgu") },
        { "MT-04", (843, "Birkirkara") },
        { "MT-05", (2118, "Birzebbuga") },
        { "MT-06", (118, "Bormla") },
        { "MT-07", (1134, "Dingli") },
        { "MT-08", (425, "Fgura") },
        { "MT-09", (65, "Floriana") },
        { "MT-10", (31, "Fontana") },
        { "MT-11", (519, "Gudja") },
        { "MT-12", (388, "Gzira") },
        { "MT-13", (1564, "Ghajnsielem") },
        { "MT-14", (886, "Gharb") },
        { "MT-15", (486, "Gharghur") },
        { "MT-16", (1099, "Ghasri") },
        { "MT-17", (783, "Ghaxaq") },
        { "MT-18", (383, "Hamrun") },
        { "MT-19", (295, "Iklin") },
        { "MT-20", (50, "Isla") },
        { "MT-21", (283, "Kalkara") },
        { "MT-22", (1121, "Kerċem") },
        { "MT-23", (281, "Kirkop") },
        { "MT-24", (274, "Lija") },
        { "MT-25", (1420, "Luqa") },
        { "MT-26", (579, "Marsa") },
        { "MT-27", (1298, "Marsaskala") },
        { "MT-28", (906, "Marsaxlokk") },
        { "MT-29", (15, "Mdina") },
        { "MT-30", (4939, "Mellieha") },
        { "MT-31", (3455, "Mgarr") },
        { "MT-32", (1749, "Mosta") },
        { "MT-33", (506, "Mqabba") },
        { "MT-34", (442, "Msida") },
        { "MT-35", (63, "Mtarfa") },
        { "MT-36", (460, "Munxar") },
        { "MT-37", (1612, "Nadur") },
        { "MT-38", (2621, "Naxxar") },
        { "MT-39", (631, "Paola") },
        { "MT-40", (497, "Pembroke") },
        { "MT-41", (90, "Pieta") },
        { "MT-42", (1119, "Qala") },
        { "MT-43", (1423, "Qormi") },
        { "MT-44", (928, "Qrendi") },
        { "MT-45", (615, "Rabat Gozo") },
        { "MT-46", (5665, "Rabat Malta") },
        { "MT-47", (494, "Safi") },
        { "MT-48", (425, "Saint Julian's") },
        { "MT-49", (682, "Saint John") },
        { "MT-50", (654, "Saint Lawrence") },
        { "MT-51", (3571, "Saint Paul's Bay") },
        { "MT-52", (688, "Sannat") },
        { "MT-53", (82, "Saint Lucia's") },
        { "MT-54", (155, "Santa Venera") },
        { "MT-55", (4257, "Siggiewi") },
        { "MT-56", (565, "Sliema") },
        { "MT-57", (877, "Swieqi") },
        { "MT-58", (47, "Ta' Xbiex") },
        { "MT-59", (177, "Tarxien") },
        { "MT-60", (117, "Valletta") },
        { "MT-61", (1666, "Xaghra") },
        { "MT-62", (985, "Xewkija") },
        { "MT-63", (247, "Xghajra") },
        { "MT-64", (1345, "Zabbar") },
        { "MT-65", (533, "Zebbug Gozo") },
        { "MT-66", (533, "Żebbuġ Malta") },
        { "MT-67", (1714, "Zejtun") },
        { "MT-68", (2348, "Zurrieq") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MX = new()
    {
        { "MX-AGU", (1250, "Estado de Aguascalientes") },
        { "MX-BCN", (3450, "Estado de Baja California") },
        { "MX-BCS", (3450, "Estado de Baja California Sur") },
        { "MX-CAM", (3800, "Estado de Campeche") },
        { "MX-CHH", (4300, "Estado de Chihuahua") },
        { "MX-CHP", (4600, "Estado de Chiapas") },
        { "MX-CMX", (450, "Ciudad de Mexico") },
        { "MX-COA", (3800, "Estado de Coahuila de Zaragoza") },
        { "MX-COL", (1350, "Estado de Colima") },
        { "MX-DUR", (4200, "Estado de Durango") },
        { "MX-GRO", (3150, "Estado de Guerrero") },
        { "MX-GUA", (3050, "Estado de Guanajuato") },
        { "MX-HID", (2400, "Estado de Hidalgo") },
        { "MX-JAL", (4600, "Estado de Jalisco") },
        { "MX-MEX", (2600, "Estado de Mexico") },
        { "MX-MIC", (3700, "Estado de Michoacan de Ocampo") },
        { "MX-MOR", (1300, "Estado de Morelos") },
        { "MX-NAY", (2500, "Estado de Nayarit") },
        { "MX-NLE", (3900, "Estado de Nuevo Leon") },
        { "MX-OAX", (4700, "Estado de Oaxaca") },
        { "MX-PUE", (3700, "Estado de Puebla") },
        { "MX-QUE", (1800, "Estado de Queretaro") },
        { "MX-ROO", (4100, "Estado de Quintana Roo") },
        { "MX-SIN", (3500, "Estado de Sinaloa") },
        { "MX-SLP", (4100, "Estado de San Luis Potosi") },
        { "MX-SON", (4200, "Estado de Sonora") },
        { "MX-TAB", (3200, "Estado de Tabasco") },
        { "MX-TAM", (4000, "Estado de Tamaulipas") },
        { "MX-TLA", (1200, "Estado de Tlaxcala") },
        { "MX-VER", (5000, "Estado de Veracruz-Llave") },
        { "MX-YUC", (4000, "Estado de Yucatan") },
        { "MX-ZAC", (3800, "Estado de Zacatecas") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MY = new()
    {
        { "MY-01", (7650, "Johor") },
        { "MY-02", (5100, "Kedah") },
        { "MY-03", (6630, "Kelantan") },
        { "MY-04", (2550, "Melaka") },
        { "MY-05", (4080, "Negeri Sembilan") },
        { "MY-06", (9690, "Pahang") },
        { "MY-07", (2550, "Pulau Pinang") },
        { "MY-08", (7650, "Perak") },
        { "MY-09", (2550, "Perlis") },
        { "MY-10", (5100, "Selangor") },
        { "MY-11", (6630, "Terengganu") },
        { "MY-12", (18360, "Sabah") },
        { "MY-13", (21420, "Sarawak") },
        { "MY-14", (550, "Kuala Lumpur") },
        { "MY-15", (155, "Labuan") },
        { "MY-16", (120, "Putrajaya") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> NA = new()
    {
        { "NA-ER", (10, "Erongo") },
        { "NA-HA", (10, "Hardap") },
        { "NA-KA", (10, "Karas") },
        { "NA-KE", (10, "Kavango East") },
        { "NA-KW", (10, "Kavango West") },
        { "NA-KH", (10, "Khomas") },
        { "NA-KU", (10, "Kunene") },
        { "NA-OW", (10, "Ohangwena") },
        { "NA-OH", (10, "Omaheke") },
        { "NA-OS", (10, "Omusati") },
        { "NA-ON", (10, "Oshana") },
        { "NA-OT", (10, "Oshikoto") },
        { "NA-OD", (10, "Otjozondjupa") },
        { "NA-CA", (10, "Zambezi") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> NG = new()
    {
        { "NG-AB", (250, "Abia") },
        { "NG-AD", (650, "Adamawa") },
        { "NG-AK", (477, "Akwa Ibom") },
        { "NG-AN", (354, "Anambra") },
        { "NG-BA", (1100, "Bauchi") },
        { "NG-BE", (1198, "Benue") },
        { "NG-BO", (1, "Borno") },
        { "NG-BY", (70, "Bayelsa") },
        { "NG-CR", (823, "Cross River") },
        { "NG-DE", (761, "Delta") },
        { "NG-EB", (555, "Ebonyi") },
        { "NG-ED", (887, "Edo") },
        { "NG-EK", (300, "Ekiti") },
        { "NG-EN", (702, "Enugu") },
        { "NG-FC", (895, "Abuja Federal Capital Territory") },
        { "NG-GO", (198, "Gombe") },
        { "NG-IM", (240, "Imo") },
        { "NG-JI", (410, "Jigawa") },
        { "NG-KD", (1959, "Kaduna") },
        { "NG-KE", (180, "Kebbi") },
        { "NG-KN", (1550, "Kano") },
        { "NG-KO", (490, "Kogi") },
        { "NG-KT", (130, "Katsina") },
        { "NG-KW", (840, "Kwara") },
        { "NG-LA", (965, "Lagos") },
        { "NG-NA", (670, "Nasarawa") },
        { "NG-NI", (1540, "Niger") },
        { "NG-OG", (942, "Ogun") },
        { "NG-ON", (320, "Ondo") },
        { "NG-OS", (940, "Osun") },
        { "NG-OY", (1100, "Oyo") },
        { "NG-PL", (740, "Plateau") },
        { "NG-RI", (540, "Rivers") },
        { "NG-SO", (262, "Sokoto") },
        { "NG-TA", (323, "Taraba") },
        { "NG-YO", (271, "Yobe") },
        { "NG-ZA", (170, "Zamfara") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> NL = new()
    {
        { "NL-DR", (80, "Provincie Drenthe") },
        { "NL-FL", (45, "Provincie Flevoland") },
        { "NL-FR", (90, "Provincie Friesland") },
        { "NL-GE", (150, "Provincie Gelderland") },
        { "NL-GR", (100, "Provincie Groningen") },
        { "NL-LI", (90, "Provincie Limburg") },
        { "NL-NB", (150, "Provincie Noord-Brabant") },
        { "NL-NH", (110, "Provincie Noord-Holland") },
        { "NL-OV", (110, "Provincie Overijssel") },
        { "NL-UT", (60, "Provincie Utrecht") },
        { "NL-ZE", (55, "Provincie Zeeland") },
        { "NL-ZH", (100, "Provincie Zuid-Holland") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> NO = new()
    {
        { "NO-03", (270, "Oslo") },
        { "NO-11", (3300, "Rogaland") },
        { "NO-15", (3500, "Møre og Romsdal") },
        { "NO-18", (5800, "Nordland") },
        { "NO-21", (30, "Svalbard") },
        { "NO-31", (1000, "Østfold") },
        { "NO-32", (1150, "Akershus") },
        { "NO-33", (2650, "Buskerud") },
        { "NO-34", (6300, "Innlandet") },
        { "NO-39", (650, "Vestfold") },
        { "NO-40", (3150, "Telemark") },
        { "NO-42", (3800, "Agder") },
        { "NO-46", (5800, "Vestland") },
        { "NO-50", (6000, "Trøndelag") },
        { "NO-55", (3650, "Troms") },
        { "NO-56", (3550, "Finnmark") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> NP = new()
    {
        { "NP-P3", (10, "Bāgmatī") },
        { "NP-P4", (10, "Gaṇḍakī") },
        { "NP-P6", (10, "Karṇālī") },
        { "NP-P1", (10, "Koshī") },
        { "NP-P5", (10, "Lumbinī") },
        { "NP-P2", (10, "Madhesh") },
        { "NP-P7", (10, "Sudūrpashchim") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> NZ = new()
    {
        { "NZ-AUK", (2700, "Auckland") },
        { "NZ-BOP", (3100, "Bay of Plenty") },
        { "NZ-CAN", (5300, "Canterbury") },
        { "NZ-GIS", (2250, "Gisborne") },
        { "NZ-HKB", (3100, "Hawke's Bay") },
        { "NZ-MBH", (2500, "Marlborough") },
        { "NZ-MWT", (3850, "Manawatu-Whanganui") },
        { "NZ-NSN", (350, "Nelson") },
        { "NZ-NTL", (3100, "Northland") },
        { "NZ-OTA", (4050, "Otago") },
        { "NZ-STL", (3950, "Southland") },
        { "NZ-TAS", (2500, "Tasman") },
        { "NZ-TKI", (2550, "Taranaki") },
        { "NZ-WGN", (2850, "Greater Wellington") },
        { "NZ-WKO", (3950, "Waikato") },
        { "NZ-WTC", (3900, "West Coast") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> OM = new()
    {
        { "OM-DA", (520, "Interior") },
        { "OM-BU", (227, "Buraymi") },
        { "OM-WU", (338, "Central") },
        { "OM-ZA", (338, "Dhahira") },
        { "OM-BJ", (204, "South Batina") },
        { "OM-SJ", (339, "Southeastern") },
        { "OM-MA", (268, "Muscat") },
        { "OM-MU", (12, "Musandam") },
        { "OM-BS", (596, "North Batina") },
        { "OM-SS", (273, "Northeastern") },
        { "OM-ZU", (762, "Dhofar") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PA = new()
    {
        { "PA-1", (43, "Bocas del Toro") },
        { "PA-10", (124, "Panamá Oeste") },
        { "PA-2", (158, "Cocle") },
        { "PA-3", (69, "Colon") },
        { "PA-4", (268, "Chiriqui") },
        { "PA-5", (30, "Darien") },
        { "PA-6", (69, "Herrera") },
        { "PA-7", (104, "Los Santos") },
        { "PA-8", (128, "Panama") },
        { "PA-9", (201, "Veraguas") },
        { "PA-NB", (35, "Ngobe-Bugle") },
        { "PA-NT", (1, "Naso Tjër Di") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PE = new()
    {
        { "PE-AMA", (180, "Amazonas") },
        { "PE-ANC", (563, "Ancash") },
        { "PE-APU", (165, "Region de Apurimac") },
        { "PE-ARE", (822, "Arequipa") },
        { "PE-AYA", (266, "Ayacucho") },
        { "PE-CAJ", (587, "Cajamarca") },
        { "PE-CAL", (37, "Callao") },
        { "PE-CUS", (426, "Cusco") },
        { "PE-HUC", (310, "Region de Huanuco") },
        { "PE-HUV", (261, "Huancavelica") },
        { "PE-ICA", (343, "Ica") },
        { "PE-JUN", (512, "Junin") },
        { "PE-LAL", (641, "La Libertad") },
        { "PE-LAM", (386, "Lambayeque") },
        { "PE-LIM", (962, "Lima") },
        { "PE-LOR", (58, "Loreto") },
        { "PE-MDD", (182, "Madre de Dios") },
        { "PE-MOQ", (199, "Departamento de Moquegua") },
        { "PE-PAS", (222, "Pasco") },
        { "PE-PIU", (571, "Piura") },
        { "PE-PUN", (668, "Puno") },
        { "PE-SAM", (489, "Region de San Martin") },
        { "PE-TAC", (264, "Tacna") },
        { "PE-TUM", (133, "Tumbes") },
        { "PE-UCA", (127, "Ucayali") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PH = new()
    {
        { "PH-00", (984, "National Capital Region") },
        { "PH-01", (7246, "Ilocos") },
        { "PH-02", (5662, "Cagayan Valley") },
        { "PH-03", (11112, "Central Luzon") },
        { "PH-05", (7180, "Bicol") },
        { "PH-06", (10251, "Western Visayas") },
        { "PH-07", (10302, "Central Visayas") },
        { "PH-08", (5710, "Eastern Visayas") },
        { "PH-09", (2928, "Zamboanga del Norte") },
        { "PH-10", (5572, "Northern Mindanao") },
        { "PH-11", (5863, "Davao") },
        { "PH-12", (5078, "Soccsksargen") },
        { "PH-13", (2065, "Caraga") },
        { "PH-14", (648, "Autonomous Region in Muslim Mindanao") },
        { "PH-15", (2730, "Cordillera Administrative Region") },
        { "PH-40", (8876, "Calabarzon") },
        { "PH-41", (6153, "Mimaropa") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PK = new()
    {
        { "PK-PB", (7, "Punjab") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PL = new()
    {
        { "PL-02", (8253, "Lower Silesian Voivodeship") },
        { "PL-04", (8998, "Kuyavian-Pomeranian Voivodeship") },
        { "PL-06", (7683, "Lublin Voivodeship") },
        { "PL-08", (3933, "Lubusz Voivodeship") },
        { "PL-10", (7876, "Wojewodztwo Lodzkie") },
        { "PL-12", (8618, "Lesser Poland Voivodeship") },
        { "PL-14", (14713, "Masovian Voivodeship") },
        { "PL-16", (2849, "Opole Voivodeship") },
        { "PL-18", (8213, "Podkarpackie Voivodeship") },
        { "PL-20", (6997, "Podlaskie Voivodeship") },
        { "PL-22", (6267, "Pomeranian Voivodeship") },
        { "PL-24", (8289, "Silesian Voivodeship") },
        { "PL-26", (4507, "Wojewodztwo Swietokrzyskie") },
        { "PL-28", (5538, "Warmian-Masurian Voivodeship") },
        { "PL-30", (14305, "Greater Poland Voivodeship") },
        { "PL-32", (7089, "West Pomeranian Voivodeship") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PN = new()
    {
        { "PN-1", (1, "Pitcairn") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PR = new()
    {
        { "US-PR", (1250, "Puerto Rico") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PS = new()
    {
        { "Area A", (97, "Area A") },
        { "Area B", (16, "Area B") },
        { "Judea and Samaria", (500, "Judea and Samaria") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PT = new()
    {
        { "PT-01", (3384, "Distrito de Aveiro") },
        { "PT-02", (3397, "Distrito de Beja") },
        { "PT-03", (3228, "Distrito de Braga") },
        { "PT-04", (3155, "Distrito de Braganca") },
        { "PT-05", (3691, "Distrito de Castelo Branco") },
        { "PT-06", (3998, "Distrito de Coimbra") },
        { "PT-07", (2259, "Distrito de Evora") },
        { "PT-08", (3691, "Distrito de Faro") },
        { "PT-09", (3195, "Distrito da Guarda") },
        { "PT-10", (4008, "Distrito de Leiria") },
        { "PT-11", (3192, "Distrito de Lisboa") },
        { "PT-12", (2093, "Distrito de Portalegre") },
        { "PT-13", (3283, "Distrito do Porto") },
        { "PT-14", (4681, "Distrito de Santarem") },
        { "PT-15", (2459, "Distrito de Setubal") },
        { "PT-16", (2110, "Distrito de Viana do Castelo") },
        { "PT-17", (3222, "Distrito de Vila Real") },
        { "PT-18", (4525, "Distrito de Viseu") },
        { "PT-20", (903, "Azores") },
        { "PT-30", (416, "Madeira") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> PY = new()
    {
        { "PY-16", (10, "Upper Paraguay") },
        { "PY-10", (10, "Upper Parana") },
        { "PY-13", (10, "Amambay") },
        { "PY-ASU", (10, "Asuncion") },
        { "PY-19", (10, "Boqueron") },
        { "PY-5", (10, "Caaguazu") },
        { "PY-6", (10, "Caazapa") },
        { "PY-14", (10, "Canindeyu") },
        { "PY-11", (10, "Central") },
        { "PY-1", (10, "Concepcion") },
        { "PY-3", (10, "Cordillera") },
        { "PY-4", (10, "Guaira") },
        { "PY-7", (10, "Itapua") },
        { "PY-8", (10, "Misiones") },
        { "PY-12", (10, "Neembucu") },
        { "PY-9", (10, "Paraguari") },
        { "PY-15", (10, "President Hayes") },
        { "PY-2", (10, "Saint Peter") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> QA = new()
    {
        { "QA-DA", (306, "Doha") },
        { "QA-KH", (356, "Baladiyat al Khawr wa adh Dhakhirah") },
        { "QA-MS", (157, "Madinat ash Shamal") },
        { "QA-RA", (602, "Baladiyat ar Rayyan") },
        { "QA-SH", (454, "Al-Shahaniya") },
        { "QA-US", (217, "Baladiyat Umm Salal") },
        { "QA-WA", (403, "Al Wakrah") },
        { "QA-ZA", (257, "Baladiyat az Za‘ayin") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> RE = new()
    {
        { "FR-974", (610, "Réunion") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> RO = new()
    {
        { "RO-AB", (2007, "Alba") },
        { "RO-AG", (1108, "Arges") },
        { "RO-AR", (1379, "Arad") },
        { "RO-B", (579, "N/A") },
        { "RO-BC", (1070, "Bacau") },
        { "RO-BH", (1455, "Bihor") },
        { "RO-BN", (953, "Bistriţa-Nasaud") },
        { "RO-BR", (538, "Braila") },
        { "RO-BT", (984, "Botosani") },
        { "RO-BV", (954, "Brasov") },
        { "RO-BZ", (1004, "Buzau") },
        { "RO-CJ", (1408, "Cluj") },
        { "RO-CL", (745, "Calarasi") },
        { "RO-CS", (1396, "Caras-Severin") },
        { "RO-CT", (1055, "Constanta") },
        { "RO-CV", (551, "Covasna") },
        { "RO-DB", (698, "Damboviţa") },
        { "RO-DJ", (1280, "Dolj") },
        { "RO-GJ", (838, "Gorj") },
        { "RO-GL", (708, "Galaţi") },
        { "RO-GR", (498, "Giurgiu") },
        { "RO-HD", (1385, "Hunedoara") },
        { "RO-HR", (1087, "Harghita") },
        { "RO-IF", (396, "Ilfov") },
        { "RO-IL", (612, "Ialomiţa") },
        { "RO-IS", (965, "Iasi") },
        { "RO-MH", (724, "Mehedinţi") },
        { "RO-MM", (1078, "Maramures") },
        { "RO-MS", (1196, "Mures") },
        { "RO-NT", (1052, "Neamţ") },
        { "RO-OT", (866, "Olt") },
        { "RO-PH", (948, "Prahova") },
        { "RO-SB", (1004, "Sibiu") },
        { "RO-SJ", (703, "Salaj") },
        { "RO-SM", (750, "Satu Mare") },
        { "RO-SV", (1366, "Suceava") },
        { "RO-TL", (1233, "Tulcea") },
        { "RO-TM", (1681, "Timis") },
        { "RO-TR", (913, "Teleorman") },
        { "RO-VL", (1042, "Valcea") },
        { "RO-VN", (767, "Vrancea") },
        { "RO-VS", (1026, "Vaslui") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> RS = new()
    {
        { "RS-00", (1573, "Beograd") },
        { "RS-01", (553, "Severnobacki okrug") },
        { "RS-02", (918, "Srednjebanatski okrug") },
        { "RS-03", (665, "Severnobanatski okrug") },
        { "RS-04", (1222, "Juznobanatski okrug") },
        { "RS-05", (702, "Zapadnobacki okrug") },
        { "RS-06", (1405, "Juznobacki okrug") },
        { "RS-07", (1123, "Sremski okrug") },
        { "RS-08", (958, "Macvanski okrug") },
        { "RS-09", (742, "Kolubarski okrug") },
        { "RS-10", (435, "Podunavski okrug") },
        { "RS-11", (1048, "Branicevski okrug") },
        { "RS-12", (751, "Sumadijski okrug") },
        { "RS-13", (802, "Pomoravski okrug") },
        { "RS-14", (969, "Borski okrug") },
        { "RS-15", (1031, "Zajecarski okrug") },
        { "RS-16", (1696, "Zlatiborski okrug") },
        { "RS-17", (903, "Moravicki okrug") },
        { "RS-18", (1153, "Raski okrug") },
        { "RS-19", (799, "Rasinski okrug") },
        { "RS-20", (960, "Nisavski okrug") },
        { "RS-21", (664, "Toplicki okrug") },
        { "RS-22", (762, "Pirotski okrug") },
        { "RS-23", (855, "Jablanicki okrug") },
        { "RS-24", (1013, "Pcinjski okrug") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> RU = new()
    {
        { "RU-AD", (586, "Adygeya, Respublika") },
        { "RU-AL", (1245, "Altay, Respublika") },
        { "RU-ALT", (1611, "Altayskiy kray") },
        { "RU-AMU", (1684, "Amurskaya oblast") },
        { "RU-ARK", (1831, "Arkhangelskaya oblast") },
        { "RU-AST", (879, "Astrakhanskaya oblast") },
        { "RU-BA", (1538, "Bashkortostan, Respublika") },
        { "RU-BEL", (879, "Belgorodskaya oblast") },
        { "RU-BRY", (879, "Bryanskaya oblast") },
        { "RU-BU", (1904, "Buryatiya, Respublika") },
        { "RU-CE", (662, "Chechenskaya Respublika") },
        { "RU-CHE", (1904, "Chelyabinskaya oblast") },
        { "RU-CU", (710, "Chuvashskaya Respublika") },
        { "RU-DA", (1098, "Dagestan, Respublika") },
        { "RU-IN", (198, "Ingushetiya, Respublika") },
        { "RU-IRK", (1831, "Irkutskaya oblast") },
        { "RU-IVA", (732, "Ivanovskaya oblast") },
        { "RU-KAM", (1465, "Kamchatskiy kray") },
        { "RU-KB", (732, "Kabardino-Balkarskaya Respublika") },
        { "RU-KC", (622, "Karachayevo-Cherkesskaya Respublika") },
        { "RU-KDA", (1904, "Krasnodarskiy kray") },
        { "RU-KEM", (1684, "Kemerovskaya oblast") },
        { "RU-KGD", (1611, "Kaliningradskaya oblast") },
        { "RU-KGN", (1245, "Kurganskaya oblast") },
        { "RU-KHA", (1391, "Khabarovskiy kray") },
        { "RU-KHM", (1977, "Khanty-Mansiyskiy avtonomnyy okrug") },
        { "RU-KIR", (1501, "Kirovskaya oblast") },
        { "RU-KK", (1538, "Khakasiya, Respublika") },
        { "RU-KL", (915, "Kalmykiya, Respublika") },
        { "RU-KLU", (732, "Kaluzhskaya oblast") },
        { "RU-KO", (1904, "Komi, Respublika") },
        { "RU-KOS", (806, "Kostromskaya oblast") },
        { "RU-KR", (1684, "Kareliya, Respublika") },
        { "RU-KRS", (732, "Kurskaya oblast") },
        { "RU-KYA", (1684, "Krasnoyarskiy kray") },
        { "RU-LEN", (1831, "Leningradskaya oblast") },
        { "RU-LIP", (732, "Lipetskaya oblast") },
        { "RU-MAG", (1538, "Magadanskaya oblast") },
        { "RU-ME", (710, "Mariy El, Respublika") },
        { "RU-MO", (718, "Mordoviya, Respublika") },
        { "RU-MOS", (1611, "Moskovskaya oblast") },
        { "RU-MOW", (220, "Moskva") },
        { "RU-MUR", (1757, "Murmanskaya oblast") },
        { "RU-NEN", (29, "Nenetskiy avtonomnyy okrug") },
        { "RU-NGR", (1098, "Novgorodskaya oblast") },
        { "RU-NIZ", (1391, "Nizhegorodskaya oblast") },
        { "RU-NVS", (1538, "Novosibirskaya oblast") },
        { "RU-OMS", (1684, "Omskaya oblast") },
        { "RU-ORE", (1538, "Orenburgskaya oblast") },
        { "RU-ORL", (696, "Orlovskaya oblast") },
        { "RU-PER", (1538, "Permskiy kray") },
        { "RU-PNZ", (806, "Penzenskaya oblast") },
        { "RU-PRI", (1757, "Primorskiy kray") },
        { "RU-PSK", (1098, "Pskovskaya oblast") },
        { "RU-ROS", (1465, "Rostovskaya oblast") },
        { "RU-RYA", (806, "Ryazanskaya oblast") },
        { "RU-SA", (2197, "Saha, Respublika") },
        { "RU-SAK", (1684, "Sakhalinskaya oblast") },
        { "RU-SAM", (1175, "Samarskaya oblast") },
        { "RU-SAR", (1391, "Saratovskaya oblast") },
        { "RU-SE", (659, "Severnaya Osetiya, Respublika") },
        { "RU-SMO", (842, "Smolenskaya oblast") },
        { "RU-SPE", (110, "Sankt-Peterburg") },
        { "RU-STA", (1245, "Stavropolskiy kray") },
        { "RU-SVE", (1684, "Sverdlovskaya oblast") },
        { "RU-TA", (1391, "Tatarstan, Respublika") },
        { "RU-TAM", (732, "Tambovskaya oblast") },
        { "RU-TOM", (1684, "Tomskaya oblast") },
        { "RU-TUL", (879, "Tulskaya oblast") },
        { "RU-TVE", (1318, "Tverskaya oblast") },
        { "RU-TY", (1245, "Tyva, Respublika") },
        { "RU-TYU", (1465, "Tyumenskaya oblast") },
        { "RU-UD", (879, "Udmurtskaya Respublika") },
        { "RU-ULY", (879, "Ulyanovskaya oblast") },
        { "RU-VGG", (1465, "Volgogradskaya oblast") },
        { "RU-VLA", (806, "Vladimirskaya oblast") },
        { "RU-VLG", (1025, "Vologodskaya oblast") },
        { "RU-VOR", (952, "Voronezhskaya oblast") },
        { "RU-YAN", (1684, "Yamalo-Nenetskiy avtonomnyy okrug") },
        { "RU-YAR", (879, "Yaroslavskaya oblast") },
        { "RU-YEV", (1025, "Yevreyskaya avtonomnaya oblast") },
        { "RU-ZAB", (1684, "Zabaykalskiy kray") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> RW = new()
    {
        { "RW-01", (1001, "City of Kigali") },
        { "RW-02", (584, "Eastern") },
        { "RW-03", (139, "Northern") },
        { "RW-04", (516, "Western") },
        { "RW-05", (339, "Southern") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SE = new()
    {
        { "SE-AB", (1000, "Stockholm") },
        { "SE-AC", (1500, "Vasterbotten") },
        { "SE-BD", (2000, "Norrbotten") },
        { "SE-C", (600, "Uppsala") },
        { "SE-D", (600, "Sodermanland") },
        { "SE-E", (700, "Ostergotland") },
        { "SE-F", (700, "Jonkoping") },
        { "SE-G", (700, "Kronoberg") },
        { "SE-H", (800, "Kalmar") },
        { "SE-I", (600, "Gotland") },
        { "SE-K", (500, "Blekinge") },
        { "SE-M", (1000, "Skane") },
        { "SE-N", (800, "Halland") },
        { "SE-O", (1100, "Vastra Gotaland") },
        { "SE-S", (1200, "Varmland") },
        { "SE-T", (600, "Orebro") },
        { "SE-U", (500, "Vastmanland") },
        { "SE-W", (1200, "Dalarna") },
        { "SE-X", (1000, "Gavleborg") },
        { "SE-Y", (1000, "Vasternorrland") },
        { "SE-Z", (1100, "Jamtland") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SG = new()
    {
        { "SG-01", (190, "Central Singapore") },
        { "SG-02", (112, "North East") },
        { "SG-03", (159, "North West") },
        { "SG-04", (159, "South East") },
        { "SG-05", (280, "South West") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SI = new()
    {
        { "SI-001", (2517, "Ajdovscina") },
        { "SI-002", (673, "Beltinci") },
        { "SI-003", (771, "Bled") },
        { "SI-004", (191, "Bohinj") },
        { "SI-005", (438, "Borovnica") },
        { "SI-006", (3251, "Bovec") },
        { "SI-007", (754, "Brda") },
        { "SI-008", (1004, "Brezovica") },
        { "SI-009", (2836, "Brezice") },
        { "SI-010", (395, "Tisina") },
        { "SI-011", (1595, "Celje") },
        { "SI-012", (894, "Cerklje na Gorenjskem") },
        { "SI-013", (2360, "Cerknica") },
        { "SI-014", (1301, "Cerkno") },
        { "SI-015", (348, "Crensovci") },
        { "SI-016", (1416, "Črna na Koroškem") },
        { "SI-017", (3241, "Crnomelj") },
        { "SI-018", (351, "Destrnik") },
        { "SI-019", (1434, "Divaca") },
        { "SI-020", (916, "Dobrepolje") },
        { "SI-021", (1160, "Dobrova-Polhov Gradec") },
        { "SI-022", (396, "Dol pri Ljubljani") },
        { "SI-023", (1143, "Domzale") },
        { "SI-024", (317, "Dornava") },
        { "SI-025", (1113, "Dravograd") },
        { "SI-026", (472, "Duplek") },
        { "SI-027", (1564, "Gorenja vas-Poljane") },
        { "SI-028", (610, "Gorišnica") },
        { "SI-029", (839, "Gornja Radgona") },
        { "SI-030", (857, "Gornji Grad") },
        { "SI-031", (700, "Gornji Petrovci") },
        { "SI-032", (1518, "Grosuplje") },
        { "SI-033", (697, "Salovci") },
        { "SI-034", (689, "Hrastnik") },
        { "SI-035", (1811, "Hrpelje-Kozina") },
        { "SI-036", (2745, "Idrija") },
        { "SI-037", (1051, "Ig") },
        { "SI-038", (4519, "Ilirska Bistrica") },
        { "SI-039", (2343, "Ivancna Gorica") },
        { "SI-040", (492, "Izola") },
        { "SI-041", (940, "Jesenice") },
        { "SI-042", (372, "Jursinci") },
        { "SI-043", (2828, "Kamnik") },
        { "SI-044", (1423, "Kanal") },
        { "SI-045", (716, "Kidricevo") },
        { "SI-046", (1760, "Kobarid") },
        { "SI-047", (179, "Kobilje") },
        { "SI-048", (5303, "Kocevje") },
        { "SI-049", (1022, "Komen") },
        { "SI-050", (3558, "Koper") },
        { "SI-051", (864, "Kozje") },
        { "SI-052", (872, "Kranj") },
        { "SI-053", (2328, "Kranjska Gora") },
        { "SI-054", (3156, "Krsko") },
        { "SI-055", (491, "Kungota") },
        { "SI-056", (240, "Kuzma") },
        { "SI-057", (2070, "Lasko") },
        { "SI-058", (710, "Lenart") },
        { "SI-059", (1267, "Lendava") },
        { "SI-060", (2292, "Litija") },
        { "SI-061", (6041, "Ljubljana") },
        { "SI-062", (719, "Ljubno") },
        { "SI-063", (1262, "Ljutomer") },
        { "SI-064", (1766, "Logatec") },
        { "SI-065", (1566, "Loska dolina") },
        { "SI-066", (1262, "Loski Potok") },
        { "SI-067", (1010, "Luce") },
        { "SI-068", (809, "Lukovica") },
        { "SI-069", (715, "Majsperk") },
        { "SI-070", (2767, "Maribor") },
        { "SI-071", (911, "Medvode") },
        { "SI-072", (301, "Menges") },
        { "SI-073", (1100, "Metlika") },
        { "SI-074", (276, "Mezica") },
        { "SI-075", (646, "Miren-Kostanjevica") },
        { "SI-076", (1098, "Mislinja") },
        { "SI-077", (654, "Moravce") },
        { "SI-078", (1463, "Moravske Toplice") },
        { "SI-079", (572, "Mozirje") },
        { "SI-080", (832, "Murska Sobota") },
        { "SI-081", (399, "Muta") },
        { "SI-082", (331, "Naklo") },
        { "SI-083", (441, "Nazarje") },
        { "SI-084", (2949, "Nova Gorica") },
        { "SI-085", (3196, "Novo Mesto") },
        { "SI-086", (78, "Odranci") },
        { "SI-087", (1520, "Ormoz") },
        { "SI-088", (332, "Osilnica") },
        { "SI-089", (858, "Pesnica") },
        { "SI-090", (673, "Piran") },
        { "SI-091", (2128, "Pivka") },
        { "SI-092", (614, "Podcetrtek") },
        { "SI-093", (945, "Podvelka") },
        { "SI-094", (2613, "Postojna") },
        { "SI-095", (839, "Preddvor") },
        { "SI-096", (538, "Ptuj") },
        { "SI-097", (1149, "Puconci") },
        { "SI-098", (554, "Race-Fram") },
        { "SI-099", (545, "Radece") },
        { "SI-100", (408, "Radenci") },
        { "SI-101", (897, "Radlje ob Dravi") },
        { "SI-102", (1366, "Radovljica") },
        { "SI-103", (747, "Ravne na Koroskem") },
        { "SI-104", (1518, "Ribnica") },
        { "SI-105", (455, "Rogasovci") },
        { "SI-106", (804, "Rogaska Slatina") },
        { "SI-107", (388, "Rogatec") },
        { "SI-108", (621, "Ruse") },
        { "SI-109", (1349, "Semic") },
        { "SI-110", (2696, "Sevnica") },
        { "SI-111", (2271, "Sezana") },
        { "SI-112", (1929, "Slovenj Gradec") },
        { "SI-113", (2815, "Slovenska Bistrica") },
        { "SI-114", (1214, "Slovenske Konjice") },
        { "SI-115", (368, "Starse") },
        { "SI-116", (76, "Sveti Jurij ob Scavnici") },
        { "SI-117", (510, "Sencur") },
        { "SI-118", (723, "Sentilj") },
        { "SI-119", (989, "Sentjernej") },
        { "SI-120", (2461, "Sentjur") },
        { "SI-121", (584, "Skocjan") },
        { "SI-122", (1701, "Skofja Loka") },
        { "SI-123", (582, "Skofljica") },
        { "SI-124", (1178, "Smarje pri Jelsah") },
        { "SI-125", (243, "Smartno ob Paki") },
        { "SI-126", (1098, "Sostanj") },
        { "SI-127", (351, "Store") },
        { "SI-128", (3584, "Tolmin") },
        { "SI-129", (771, "Trbovlje") },
        { "SI-130", (2069, "Trebnje") },
        { "SI-131", (1588, "Trzic") },
        { "SI-132", (258, "Turnisce") },
        { "SI-133", (1291, "Velenje") },
        { "SI-134", (1056, "Velike Lasce") },
        { "SI-135", (809, "Videm") },
        { "SI-136", (1065, "Vipava") },
        { "SI-137", (628, "Vitanje") },
        { "SI-138", (398, "Vodice") },
        { "SI-139", (927, "Vojnik") },
        { "SI-140", (1306, "Vrhnika") },
        { "SI-141", (490, "Vuzenica") },
        { "SI-142", (1644, "Zagorje ob Savi") },
        { "SI-143", (188, "Zavrc") },
        { "SI-144", (735, "Zrece") },
        { "SI-146", (1588, "Zelezniki") },
        { "SI-147", (523, "Ziri") },
        { "SI-148", (260, "Benedikt") },
        { "SI-149", (297, "Bistrica ob Sotli") },
        { "SI-150", (771, "Bloke") },
        { "SI-151", (606, "Braslovce") },
        { "SI-152", (316, "Cankova") },
        { "SI-153", (242, "Cerkvenjak") },
        { "SI-154", (171, "Dobje") },
        { "SI-155", (343, "Dobrna") },
        { "SI-156", (308, "Dobrovnik") },
        { "SI-157", (1032, "Dolenjske Toplice") },
        { "SI-158", (418, "Grad") },
        { "SI-159", (264, "Hajdina") },
        { "SI-160", (646, "Hoce-Slivnica") },
        { "SI-161", (176, "Hodos") },
        { "SI-162", (344, "Horjul") },
        { "SI-163", (608, "Jezersko") },
        { "SI-164", (318, "Komenda") },
        { "SI-165", (527, "Kostel") },
        { "SI-166", (487, "Krizevci") },
        { "SI-167", (773, "Lovrenc na Pohorju") },
        { "SI-168", (326, "Markovci") },
        { "SI-169", (193, "Miklavz na Dravskem polju") },
        { "SI-170", (517, "Mirna Pec") },
        { "SI-171", (389, "Oplotnica") },
        { "SI-172", (448, "Podlehnik") },
        { "SI-173", (436, "Polzela") },
        { "SI-174", (503, "Prebold") },
        { "SI-175", (600, "Prevalje") },
        { "SI-176", (105, "Razkrizje") },
        { "SI-177", (534, "Ribnica na Pohorju") },
        { "SI-178", (615, "Selnica ob Dravi") },
        { "SI-179", (516, "Sodrazica") },
        { "SI-180", (913, "Solcava") },
        { "SI-181", (375, "Sveta Ana") },
        { "SI-182", (183, "Sveti Andraz v Slovenskih Goricah") },
        { "SI-183", (227, "Sempeter-Vrtojba") },
        { "SI-184", (352, "Tabor") },
        { "SI-185", (215, "Trnovska Vas") },
        { "SI-186", (124, "Trzin") },
        { "SI-187", (179, "Velika Polana") },
        { "SI-188", (126, "Verzej") },
        { "SI-189", (544, "Vransko") },
        { "SI-190", (1518, "Zalec") },
        { "SI-191", (357, "Zetale") },
        { "SI-192", (426, "Žirovnica") },
        { "SI-193", (1517, "Zuzemberk") },
        { "SI-194", (967, "Smartno pri Litiji") },
        { "SI-195", (521, "Apace") },
        { "SI-196", (311, "Cirkulane") },
        { "SI-197", (573, "Kosanjevica na Krki") },
        { "SI-198", (374, "Makole") },
        { "SI-199", (721, "Mokronog-Trebelno") },
        { "SI-200", (416, "Poljcane") },
        { "SI-201", (353, "Rence-Vogrsko") },
        { "SI-202", (325, "Središče ob Dravi") },
        { "SI-203", (409, "Straza") },
        { "SI-204", (272, "Sveta Trojica v Slovenskih goricah") },
        { "SI-205", (415, "Sveti Tomaz") },
        { "SI-206", (494, "Smarjeske Toplice") },
        { "SI-207", (1068, "Gorje") },
        { "SI-208", (154, "Log-Dragomer") },
        { "SI-209", (311, "Recica ob Savinji") },
        { "SI-210", (359, "Sveti Jurij v Slovenskih goricah") },
        { "SI-211", (498, "Sentrupert") },
        { "SI-212", (340, "Mirna") },
        { "SI-213", (121, "Ankaran") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SK = new()
    {
        { "SK-BC", (830, "Banska Bystrica") },
        { "SK-BL", (205, "Bratislava") },
        { "SK-KI", (640, "Kosice") },
        { "SK-NI", (655, "Nitra") },
        { "SK-PV", (799, "Presov") },
        { "SK-TA", (462, "Trnava") },
        { "SK-TC", (433, "Trencin") },
        { "SK-ZI", (491, "Zilina") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SM = new()
    {
        { "SM-01", (631, "Castello di Acquaviva") },
        { "SM-02", (727, "Chiesanuova") },
        { "SM-03", (955, "Domagnano") },
        { "SM-04", (1005, "Castello di Faetano") },
        { "SM-05", (884, "Castello di Fiorentino") },
        { "SM-06", (1497, "Castello di Borgo Maggiore") },
        { "SM-07", (1096, "San Marino") },
        { "SM-08", (430, "Castello di Montegiardino") },
        { "SM-09", (1775, "Serravalle") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SN = new()
    {
        { "SN-DB", (268, "Diourbel") },
        { "SN-DK", (83, "Dakar") },
        { "SN-FK", (142, "Fatick") },
        { "SN-KA", (65, "Region de Kaffrine") },
        { "SN-KD", (188, "Kolda") },
        { "SN-KE", (80, "Region de Kedougou") },
        { "SN-KL", (89, "Kaolack") },
        { "SN-LG", (245, "Louga") },
        { "SN-MT", (153, "Matam") },
        { "SN-SE", (133, "Region de Sedhiou") },
        { "SN-SL", (194, "Saint-Louis") },
        { "SN-TC", (198, "Tambacounda") },
        { "SN-TH", (406, "Region de Thies") },
        { "SN-ZG", (152, "Ziguinchor") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> ST = new()
    {
        { "ST-01", (41, "Agua Grande") },
        { "ST-02", (34, "Cantagalo") },
        { "ST-03", (41, "Caue District") },
        { "ST-04", (24, "Lemba District") },
        { "ST-05", (75, "Lobata District") },
        { "ST-06", (57, "Me-Zochi District") },
        { "ST-P", (36, "Principe") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SZ = new()
    {
        { "SZ-HH", (955, "Hhohho") },
        { "SZ-LU", (1199, "Lubombo") },
        { "SZ-MA", (1029, "Manzini") },
        { "SZ-SH", (818, "Shiselweni") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> TH = new()
    {
        { "TH-10", (247, "Bangkok") },
        { "TH-11", (141, "Changwat Samut Prakan") },
        { "TH-12", (104, "Changwat Nonthaburi") },
        { "TH-13", (231, "Changwat Pathum Thani") },
        { "TH-14", (368, "Phra Nakhon Si Ayutthaya") },
        { "TH-15", (139, "Changwat Ang Thong") },
        { "TH-16", (727, "Changwat Lop Buri") },
        { "TH-17", (125, "Changwat Sing Buri") },
        { "TH-18", (338, "Changwat Chai Nat") },
        { "TH-19", (428, "Changwat Saraburi") },
        { "TH-20", (562, "Changwat Chon Buri") },
        { "TH-21", (480, "Changwat Rayong") },
        { "TH-22", (575, "Changwat Chanthaburi") },
        { "TH-23", (309, "Changwat Trat") },
        { "TH-24", (625, "Changwat Chachoengsao") },
        { "TH-25", (467, "Changwat Prachin Buri") },
        { "TH-26", (234, "Changwat Nakhon Nayok") },
        { "TH-27", (597, "Changwat Sa Kaeo") },
        { "TH-30", (2186, "Changwat Nakhon Ratchasima") },
        { "TH-31", (1172, "Changwat Buri Ram") },
        { "TH-32", (1069, "Changwat Surin") },
        { "TH-33", (997, "Changwat Si Sa Ket") },
        { "TH-34", (1657, "Changwat Ubon Ratchathani") },
        { "TH-35", (499, "Changwat Yasothon") },
        { "TH-36", (1137, "Changwat Chaiyaphum") },
        { "TH-37", (407, "Changwat Amnat Charoen") },
        { "TH-38", (472, "Changwat Bueng Kan") },
        { "TH-39", (476, "Changwat Nong Bua Lamphu") },
        { "TH-40", (1197, "Changwat Khon Kaen") },
        { "TH-41", (1302, "Changwat Udon Thani") },
        { "TH-42", (826, "Changwat Loei") },
        { "TH-43", (428, "Changwat Nong Khai") },
        { "TH-44", (694, "Changwat Maha Sarakham") },
        { "TH-45", (928, "Changwat Roi Et") },
        { "TH-46", (731, "Changwat Kalasin") },
        { "TH-47", (1046, "Changwat Sakon Nakhon") },
        { "TH-48", (613, "Changwat Nakhon Phanom") },
        { "TH-49", (334, "Changwat Mukdahan") },
        { "TH-50", (1114, "Chiang Mai Province") },
        { "TH-51", (296, "Changwat Lamphun") },
        { "TH-52", (706, "Changwat Lampang") },
        { "TH-53", (472, "Changwat Uttaradit") },
        { "TH-54", (379, "Changwat Phrae") },
        { "TH-55", (623, "Changwat Nan") },
        { "TH-56", (393, "Changwat Phayao") },
        { "TH-57", (835, "Changwat Chiang Rai") },
        { "TH-58", (395, "Changwat Mae Hong Son") },
        { "TH-60", (1114, "Changwat Nakhon Sawan") },
        { "TH-61", (424, "Changwat Uthai Thani") },
        { "TH-62", (806, "Changwat Kamphaeng Phet") },
        { "TH-63", (548, "Changwat Tak") },
        { "TH-64", (647, "Changwat Sukhothai") },
        { "TH-65", (843, "Changwat Phitsanulok") },
        { "TH-66", (549, "Changwat Phichit") },
        { "TH-67", (962, "Changwat Phetchabun") },
        { "TH-70", (521, "Changwat Ratchaburi") },
        { "TH-71", (1034, "Changwat Kanchanaburi") },
        { "TH-72", (683, "Changwat Suphan Buri") },
        { "TH-73", (327, "Changwat Nakhon Pathom") },
        { "TH-74", (134, "Changwat Samut Sakhon") },
        { "TH-75", (63, "Changwat Samut Songkhram") },
        { "TH-76", (408, "Changwat Phetchaburi") },
        { "TH-77", (507, "Changwat Prachuap Khiri Khan") },
        { "TH-80", (1097, "Changwat Nakhon Si Thammarat") },
        { "TH-81", (517, "Changwat Krabi") },
        { "TH-82", (302, "Changwat Phangnga") },
        { "TH-83", (85, "Phuket Province") },
        { "TH-84", (1135, "Changwat Surat Thani") },
        { "TH-85", (196, "Changwat Ranong") },
        { "TH-86", (630, "Changwat Chumphon") },
        { "TH-90", (780, "Changwat Songkhla") },
        { "TH-91", (238, "Changwat Satun") },
        { "TH-92", (534, "Changwat Trang") },
        { "TH-93", (383, "Changwat Phatthalung") },
        { "TH-94", (274, "Changwat Pattani") },
        { "TH-95", (296, "Changwat Yala") },
        { "TH-96", (351, "Changwat Narathiwat") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> TN = new()
    {
        { "TN-11", (290, "Gouvernorat de Tunis") },
        { "TN-12", (77, "Gouvernorat de l’Ariana") },
        { "TN-13", (99, "Gouvernorat de Ben Arous") },
        { "TN-14", (22, "Manouba") },
        { "TN-21", (150, "Gouvernorat de Nabeul") },
        { "TN-23", (111, "Gouvernorat de Bizerte") },
        { "TN-41", (55, "Gouvernorat de Kairouan") },
        { "TN-51", (215, "Gouvernorat de Sousse") },
        { "TN-52", (120, "Gouvernorat de Monastir") },
        { "TN-53", (150, "Gouvernorat de Mahdia") },
        { "TN-61", (350, "Gouvernorat de Sfax") },
        { "TN-81", (97, "Gouvernorat de Gabes") },
        { "TN-82", (210, "Gouvernorat de Medenine") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> TR = new()
    {
        { "TR-01", (730, "Adana") },
        { "TR-02", (173, "Adiyaman") },
        { "TR-03", (499, "Afyonkarahisar") },
        { "TR-04", (400, "Agri") },
        { "TR-05", (230, "Amasya") },
        { "TR-06", (1334, "Ankara") },
        { "TR-07", (884, "Antalya") },
        { "TR-08", (146, "Artvin") },
        { "TR-09", (506, "Aydin") },
        { "TR-10", (822, "Balikesir") },
        { "TR-11", (286, "Bilecik") },
        { "TR-12", (110, "Bingol") },
        { "TR-13", (216, "Bitlis") },
        { "TR-14", (387, "Bolu") },
        { "TR-15", (334, "Burdur") },
        { "TR-16", (782, "Bursa") },
        { "TR-17", (689, "Canakkale") },
        { "TR-18", (255, "Cankiri") },
        { "TR-19", (629, "Corum") },
        { "TR-20", (520, "Denizli") },
        { "TR-21", (244, "Diyarbakir") },
        { "TR-22", (408, "Edirne") },
        { "TR-23", (252, "Elazig") },
        { "TR-24", (368, "Erzincan") },
        { "TR-25", (703, "Erzurum") },
        { "TR-26", (624, "Eskisehir") },
        { "TR-27", (289, "Gaziantep") },
        { "TR-28", (153, "Giresun") },
        { "TR-29", (134, "Gumushane") },
        { "TR-30", (74, "Hakkari") },
        { "TR-31", (359, "Hatay") },
        { "TR-32", (298, "Isparta") },
        { "TR-33", (701, "Mersin") },
        { "TR-34", (609, "Istanbul") },
        { "TR-35", (844, "Izmir") },
        { "TR-36", (323, "Kars") },
        { "TR-37", (603, "Kastamonu") },
        { "TR-38", (663, "Kayseri") },
        { "TR-39", (404, "Kirklareli") },
        { "TR-40", (303, "Kirsehir") },
        { "TR-41", (369, "Kocaeli") },
        { "TR-42", (1799, "Konya") },
        { "TR-43", (552, "Kutahya") },
        { "TR-44", (226, "Malatya") },
        { "TR-45", (600, "Manisa") },
        { "TR-46", (273, "Kahramanmaras") },
        { "TR-47", (503, "Mardin") },
        { "TR-48", (372, "Mugla") },
        { "TR-49", (267, "Mus") },
        { "TR-50", (294, "Nevsehir") },
        { "TR-51", (262, "Nigde") },
        { "TR-52", (316, "Ordu") },
        { "TR-53", (136, "Rize") },
        { "TR-54", (458, "Sakarya") },
        { "TR-55", (479, "Samsun") },
        { "TR-56", (204, "Siirt") },
        { "TR-57", (239, "Sinop") },
        { "TR-58", (419, "Sivas") },
        { "TR-59", (513, "Tekirdag") },
        { "TR-60", (459, "Tokat") },
        { "TR-61", (218, "Trabzon") },
        { "TR-62", (257, "Tunceli") },
        { "TR-63", (545, "Sanliurfa") },
        { "TR-64", (223, "Usak") },
        { "TR-65", (420, "Van") },
        { "TR-66", (331, "Yozgat") },
        { "TR-67", (223, "Zonguldak") },
        { "TR-68", (393, "Aksaray") },
        { "TR-69", (116, "Bayburt") },
        { "TR-70", (325, "Karaman") },
        { "TR-71", (214, "Kirikkale") },
        { "TR-72", (198, "Batman") },
        { "TR-73", (190, "Sirnak") },
        { "TR-74", (147, "Bartin") },
        { "TR-75", (159, "Ardahan") },
        { "TR-76", (182, "Igdir") },
        { "TR-77", (94, "Yalova") },
        { "TR-78", (160, "Karabuk") },
        { "TR-79", (56, "Kilis") },
        { "TR-80", (204, "Osmaniye") },
        { "TR-81", (143, "Duzce") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> TW = new()
    {
        { "TW-CHA", (1789, "Changhua") },
        { "TW-CYI", (111, "Chiayi") },
        { "TW-CYQ", (2039, "Chiayi County") },
        { "TW-HSQ", (1120, "Hsinchu County") },
        { "TW-HSZ", (188, "Hsinchu") },
        { "TW-HUA", (1494, "Hualien") },
        { "TW-ILA", (966, "Yilan") },
        { "TW-KEE", (180, "Keelung") },
        { "TW-KHH", (2110, "Kaohsiung") },
        { "TW-KIN", (156, "Kinmen County") },
        { "TW-LIE", (50, "Lienchiang") },
        { "TW-MIA", (1650, "Miaoli") },
        { "TW-NAN", (2213, "Nantou") },
        { "TW-NWT", (1957, "New Taipei") },
        { "TW-PEN", (186, "Penghu County") },
        { "TW-PIF", (2166, "Pingtung") },
        { "TW-TAO", (1450, "Taoyuan") },
        { "TW-TNN", (3052, "Tainan") },
        { "TW-TPE", (414, "Taipei") },
        { "TW-TTT", (1496, "Taitung") },
        { "TW-TXG", (1966, "Taichung") },
        { "TW-YUN", (1977, "Yunlin") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> UA = new()
    {
        { "UA-05", (1019, "Vinnytsya Oblast") },
        { "UA-07", (752, "Volynska Oblast") },
        { "UA-12", (1461, "Dnipropetrovsk Oblast") },
        { "UA-14", (1119, "Donetska Oblast") },
        { "UA-18", (1092, "Zhytomyrska Oblast") },
        { "UA-21", (584, "Zakarpattia Oblast") },
        { "UA-23", (1005, "Zaporizhzhya Oblast") },
        { "UA-26", (619, "Ivano-Frankivsk Oblast") },
        { "UA-30", (198, "Kyiv") },
        { "UA-32", (1222, "Kyiv Oblast") },
        { "UA-35", (932, "Kirovohrad Oblast") },
        { "UA-46", (1075, "Lvivska Oblast") },
        { "UA-48", (938, "Mykolayiv Oblast") },
        { "UA-51", (1279, "Odeska Oblast") },
        { "UA-53", (1096, "Poltava Oblast") },
        { "UA-56", (767, "Rivnenska Oblast") },
        { "UA-59", (931, "Sumska Oblast") },
        { "UA-61", (563, "Ternopil Oblast") },
        { "UA-63", (1292, "Kharkivska Oblast") },
        { "UA-65", (971, "Kherson Oblast") },
        { "UA-68", (792, "Khmelnytskyi Oblast") },
        { "UA-71", (845, "Cherkasy Oblast") },
        { "UA-74", (1136, "Chernihivska Oblast") },
        { "UA-77", (348, "Chernivtsi Oblast") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> UG = new()
    {
        { "UG-C", (971, "Central") },
        { "UG-N", (290, "Northern") },
        { "UG-W", (391, "Western") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> UM = new()
    {
        { "UM-1", (5, "United States Minor Outlying Islands") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> US = new()
    {
        { "US-AL", (1950, "Alabama") },
        { "US-AK", (1400, "Alaska") },
        { "US-AZ", (2550, "Arizona") },
        { "US-AR", (1950, "Arkansas") },
        { "US-CA", (4750, "California") },
        { "US-CO", (2750, "Colorado") },
        { "US-CT", (925, "Connecticut") },
        { "US-DE", (590, "Delaware") },
        { "US-FL", (2525, "Florida") },
        { "US-GA", (2525, "Georgia") },
        { "US-HI", (1075, "Hawaii") },
        { "US-ID", (3050, "Idaho") },
        { "US-IL", (2600, "Illinois") },
        { "US-IN", (1985, "Indiana") },
        { "US-IA", (1950, "Iowa") },
        { "US-KS", (2090, "Kansas") },
        { "US-KY", (1950, "Kentucky") },
        { "US-LA", (1850, "Louisiana") },
        { "US-ME", (1900, "Maine") },
        { "US-MD", (1175, "Maryland") },
        { "US-MA", (1075, "Massachusetts") },
        { "US-MI", (2250, "Michigan") },
        { "US-MN", (2250, "Minnesota") },
        { "US-MS", (1900, "Mississippi") },
        { "US-MO", (1925, "Missouri") },
        { "US-MT", (3200, "Montana") },
        { "US-NE", (2300, "Nebraska") },
        { "US-NV", (2300, "Nevada") },
        { "US-NH", (965, "New Hampshire") },
        { "US-NJ", (1180, "New Jersey") },
        { "US-NM", (2470, "New Mexico") },
        { "US-NY", (3325, "New York") },
        { "US-NC", (2500, "North Carolina") },
        { "US-ND", (2330, "North Dakota") },
        { "US-OH", (2575, "Ohio") },
        { "US-OK", (2280, "Oklahoma") },
        { "US-OR", (2875, "Oregon") },
        { "US-PA", (3030, "Pennsylvania") },
        { "US-RI", (475, "Rhode Island") },
        { "US-SC", (1675, "South Carolina") },
        { "US-SD", (2260, "South Dakota") },
        { "US-TN", (2390, "Tennessee") },
        { "US-TX", (4290, "Texas") },
        { "US-UT", (2680, "Utah") },
        { "US-VT", (1000, "Vermont") },
        { "US-VA", (2700, "Virginia") },
        { "US-WA", (2815, "Washington") },
        { "US-WV", (1930, "West Virginia") },
        { "US-WI", (2225, "Wisconsin") },
        { "US-WY", (2775, "Wyoming") },
        { "US-DC", (100, "District of Columbia") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> UY = new()
    {
        { "UY-AR", (250, "Departamento de Artigas") },
        { "UY-CA", (800, "Departamento de Canelones") },
        { "UY-CL", (700, "Departamento de Cerro Largo") },
        { "UY-CO", (900, "Departamento de Colonia") },
        { "UY-DU", (1000, "Departamento de Durazno") },
        { "UY-FD", (700, "Departamento de Florida") },
        { "UY-FS", (250, "Departamento de Flores") },
        { "UY-LA", (1000, "Departamento de Lavalleja") },
        { "UY-MA", (800, "Departamento de Maldonado") },
        { "UY-MO", (80, "Departamento de Montevideo") },
        { "UY-PA", (700, "Departamento de Paysandu") },
        { "UY-RN", (700, "Departamento de Rio Negro") },
        { "UY-RO", (1000, "Departamento de Rocha") },
        { "UY-RV", (800, "Departamento de Rivera") },
        { "UY-SA", (800, "Departamento de Salto") },
        { "UY-SJ", (800, "Departamento de San Jose") },
        { "UY-SO", (1000, "Departamento de Soriano") },
        { "UY-TA", (1000, "Departamento de Tacuarembo") },
        { "UY-TT", (800, "Departamento de Treinta y Tres") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> VI = new()
    {
        { "US-VI", (1, "United States Virgin Islands") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> VN = new()
    {
        { "VN-44", (10, "An Giang") },
        { "VN-43", (10, "Bà Rịa - Vũng Tàu") },
        { "VN-54", (10, "Bắc Giang") },
        { "VN-53", (10, "Bắc Kạn") },
        { "VN-55", (10, "Bạc Liêu") },
        { "VN-56", (10, "Bắc Ninh") },
        { "VN-50", (10, "Bến Tre") },
        { "VN-31", (10, "Bình Định") },
        { "VN-57", (10, "Bình Dương") },
        { "VN-58", (10, "Bình Phước") },
        { "VN-40", (10, "Bình Thuận") },
        { "VN-59", (10, "Cà Mau") },
        { "VN-CT", (10, "Cần Thơ") },
        { "VN-04", (10, "Cao Bằng") },
        { "VN-DN", (10, "Đà Nẵng") },
        { "VN-33", (10, "Đắk Lắk") },
        { "VN-72", (10, "Đắk Nông") },
        { "VN-71", (10, "Điện Biên") },
        { "VN-39", (10, "Đồng Nai") },
        { "VN-45", (10, "Đồng Tháp") },
        { "VN-30", (10, "Gia Lai") },
        { "VN-03", (10, "Hà Giang") },
        { "VN-63", (10, "Hà Nam") },
        { "VN-HN", (10, "Hà Nội") },
        { "VN-23", (10, "Hà Tĩnh") },
        { "VN-61", (10, "Hải Dương") },
        { "VN-HP", (10, "Hải Phòng") },
        { "VN-73", (10, "Hậu Giang") },
        { "VN-SG", (10, "Hồ Chí Minh") },
        { "VN-14", (10, "Hòa Bình") },
        { "VN-66", (10, "Hưng Yên") },
        { "VN-34", (10, "Khánh Hòa") },
        { "VN-47", (10, "Kiến Giang") },
        { "VN-28", (10, "Kon Tum") },
        { "VN-01", (10, "Lai Châu") },
        { "VN-35", (10, "Lâm Đồng") },
        { "VN-09", (10, "Lạng Sơn") },
        { "VN-02", (10, "Lào Cai") },
        { "VN-41", (10, "Long An") },
        { "VN-67", (10, "Nam Định") },
        { "VN-22", (10, "Nghệ An") },
        { "VN-18", (10, "Ninh Bình") },
        { "VN-36", (10, "Ninh Thuận") },
        { "VN-68", (10, "Phú Thọ") },
        { "VN-32", (10, "Phú Yên") },
        { "VN-24", (10, "Quảng Bình") },
        { "VN-27", (10, "Quảng Nam") },
        { "VN-29", (10, "Quảng Ngãi") },
        { "VN-13", (10, "Quảng Ninh") },
        { "VN-25", (10, "Quảng Trị") },
        { "VN-52", (10, "Sóc Trăng") },
        { "VN-05", (10, "Sơn La") },
        { "VN-37", (10, "Tây Ninh") },
        { "VN-20", (10, "Thái Bình") },
        { "VN-69", (10, "Thái Nguyên") },
        { "VN-21", (10, "Thanh Hóa") },
        { "VN-26", (10, "Thừa Thiên-Huế") },
        { "VN-46", (10, "Tiền Giang") },
        { "VN-51", (10, "Trà Vinh") },
        { "VN-07", (10, "Tuyên Quang") },
        { "VN-49", (10, "Vĩnh Long") },
        { "VN-70", (10, "Vĩnh Phúc") },
        { "VN-06", (10, "Yên Bái") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> ZA = new()
    {
        { "ZA-EC", (6000, "Eastern Cape") },
        { "ZA-FS", (5000, "Free State") },
        { "ZA-GP", (3500, "Gauteng") },
        { "ZA-KZN", (5750, "Kwazulu-Natal") },
        { "ZA-LP", (5750, "Limpopo") },
        { "ZA-MP", (5000, "Mpumalanga") },
        { "ZA-NC", (7500, "Northern Cape") },
        { "ZA-NW", (5000, "North-West") },
        { "ZA-WC", (6750, "Western Cape") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> UZ = new()
    {
        { "UZ-AN", (10, "Andijan") },
        { "UZ-BU", (10, "Bukhara") },
        { "UZ-FA", (10, "Fergana") },
        { "UZ-JI", (10, "Jizzakh") },
        { "UZ-NG", (10, "Namangan") },
        { "UZ-NW", (10, "Navoiy") },
        { "UZ-QA", (10, "Qashqadaryo") },
        { "UZ-QR", (10, "Karakalpakstan") },
        { "UZ-SA", (10, "Samarqand") },
        { "UZ-SI", (10, "Sirdaryo") },
        { "UZ-SU", (10, "Surkhondaryo") },
        { "UZ-TK", (10, "Tashkent City") },
        { "UZ-TO", (10, "Tashkent") },
        { "UZ-XO", (10, "Khorezm ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> TJ = new()
    {
        { "TJ-DU", (10, "Dushanbe") },
        { "TJ-KT", (10, "Khatlon") },
        { "TJ-GB", (10, "Gorno-Badakhshan") },
        { "TJ-RA", (10, "Districts under government jurisdiction") },
        { "TJ-SU", (10, "Sughd") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SA = new()
    {
        { "SA-14", (10, "Asir") },
        { "SA-11", (10, "Baha") },
        { "SA-08", (10, "Northern Borders") },
        { "SA-12", (10, "Jouf") },
        { "SA-03", (10, "Medina") },
        { "SA-05", (10, "Qassim") },
        { "SA-01", (10, "Riyadh") },
        { "SA-04", (10, "Eastern") },
        { "SA-06", (10, "Ha’il") },
        { "SA-09", (10, "Jizan") },
        { "SA-02", (10, "Mecca") },
        { "SA-10", (10, "Najran") },
        { "SA-07", (10, "Tabuk ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BH = new()
    {
        { "BH-13", (10, "Capital Governorate") },
        { "BH-14", (10, "Southern Governorate") },
        { "BH-15", (10, "Muharraq Governorate") },
        { "BH-17", (10, "Northern Governorate ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> HN = new()
    {
        { "HN-AT", (10, "Atlántida") },
        { "HN-CH", (10, "Choluteca") },
        { "HN-CL", (10, "Colón") },
        { "HN-CM", (10, "Comayagua") },
        { "HN-CP", (10, "Copán") },
        { "HN-CR", (10, "Cortés") },
        { "HN-EP", (10, "El Paraíso") },
        { "HN-FM", (10, "Francisco Morazán") },
        { "HN-GD", (10, "Gracias a Dios") },
        { "HN-IN", (10, "Intibucá") },
        { "HN-IB", (10, "Islas de la Bahía") },
        { "HN-LP", (10, "La Paz") },
        { "HN-LE", (10, "Lempira") },
        { "HN-OC", (10, "Ocotepeque") },
        { "HN-OL", (10, "Olancho") },
        { "HN-SB", (10, "Santa Bárbara") },
        { "HN-VA", (10, "Valle") },
        { "HN-YO", (10, "Yoro ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> MA = new()
    {
        { "MA-05", (10, "Béni Mellal-Khénifra") },
        { "MA-06", (10, "Casablanca-Settat") },
        { "MA-12", (10, "Dakhla-Oued Ed-Dahab") },
        { "MA-08", (10, "Drâa-Tafilalet") },
        { "MA-03", (10, "Fès-Meknès") },
        { "MA-10", (10, "Guelmim-Oued Noun") },
        { "MA-02", (10, "L'Oriental") },
        { "MA-11", (10, "Laâyoune-Sakia El Hamra") },
        { "MA-07", (10, "Marrakech-Safi") },
        { "MA-04", (10, "Rabat-Salé-Kénitra") },
        { "MA-09", (10, "Souss-Massa") },
        { "MA-01", (10, "Tanger-Tétouan-Al Hoceïma ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> KW = new()
    {
        { "KW-AH", (10, "Ahmadi") },
        { "KW-FA", (10, "Farwaniya") },
        { "KW-JA", (10, "Jahra") },
        { "KW-KU", (10, "Capital") },
        { "KW-HA", (10, "Hawalli") },
        { "KW-MU", (10, "Mubarak Al-Kabeer ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SV = new()
    {
        { "SV-AH", (10, "Ahuachapán") },
        { "SV-CA", (10, "Cabañas") },
        { "SV-CH", (10, "Chalatenango") },
        { "SV-CU", (10, "Cuscatlán") },
        { "SV-LI", (10, "La Libertad") },
        { "SV-PA", (10, "La Paz") },
        { "SV-UN", (10, "La Unión") },
        { "SV-MO", (10, "Morazán") },
        { "SV-SM", (10, "San Miguel") },
        { "SV-SS", (10, "San Salvador") },
        { "SV-SV", (10, "San Vicente") },
        { "SV-SA", (10, "Santa Ana") },
        { "SV-SO", (10, "Sonsonate") },
        { "SV-US", (10, "Usulután ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> NI = new()
    {
        { "NI-BO", (10, "Boaco") },
        { "NI-CA", (10, "Carazo") },
        { "NI-CI", (10, "Chinandega") },
        { "NI-CO", (10, "Chontales") },
        { "NI-AN", (10, "North Caribbean Coast") },
        { "NI-AS", (10, "South Caribbean Coast") },
        { "NI-ES", (10, "Estelí") },
        { "NI-GR", (10, "Granada") },
        { "NI-JI", (10, "Jinotega") },
        { "NI-LE", (10, "León") },
        { "NI-MD", (10, "Madriz") },
        { "NI-MN", (10, "Managua") },
        { "NI-MS", (10, "Masaya") },
        { "NI-MT", (10, "Matagalpa") },
        { "NI-NS", (10, "New Segovia") },
        { "NI-SJ", (10, "Saint John River") },
        { "NI-RI", (10, "Rivas ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BZ = new()
    {
        { "BZ-BZ", (10, "Belize") },
        { "BZ-CY", (10, "Cayo") },
        { "BZ-CZL", (10, "Corozal") },
        { "BZ-OW", (10, "Orange Walk") },
        { "BZ-SC", (10, "Stann Creek") },
        { "BZ-TOL", (10, "Toledo") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> JM = new()
    {
        { "JM-13", (10, "Clarendon") },
        { "JM-09", (10, "Hanover") },
        { "JM-01", (10, "Kingston") },
        { "JM-12", (10, "Manchester") },
        { "JM-04", (10, "Portland") },
        { "JM-02", (10, "Saint Andrew") },
        { "JM-06", (10, "Saint Ann") },
        { "JM-14", (10, "Saint Catherine") },
        { "JM-11", (10, "Saint Elizabeth") },
        { "JM-08", (10, "Saint James") },
        { "JM-05", (10, "Saint Mary") },
        { "JM-03", (10, "Saint Thomas") },
        { "JM-07", (10, "Trelawny") },
        { "JM-10", (10, "Westmoreland") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BS = new()
    {
        { "BS-AK", (10, "Acklins") },
        { "BS-BY", (10, "Berry Islands") },
        { "BS-BI", (10, "Bimini") },
        { "BS-BP", (10, "Black Point") },
        { "BS-CI", (10, "Cat Island") },
        { "BS-CO", (10, "Central Abaco") },
        { "BS-CS", (10, "Central Andros") },
        { "BS-CE", (10, "Central Eleuthera") },
        { "BS-FP", (10, "City of Freeport") },
        { "BS-CK", (10, "Crooked Island and Long Cay") },
        { "BS-EG", (10, "East Grand Bahama") },
        { "BS-EX", (10, "Exuma") },
        { "BS-GC", (10, "Grand Cay") },
        { "BS-HI", (10, "Harbour Island") },
        { "BS-HT", (10, "Hope Town") },
        { "BS-IN", (10, "Inagua") },
        { "BS-LI", (10, "Long Island") },
        { "BS-MC", (10, "Mangrove Cay") },
        { "BS-MG", (10, "Mayaguana") },
        { "BS-MI", (10, "Moore's Island") },
        { "BS-NP", (10, "New Providence") },
        { "BS-NO", (10, "North Abaco") },
        { "BS-NS", (10, "North Andros") },
        { "BS-NE", (10, "North Eleuthera") },
        { "BS-RI", (10, "Ragged Island") },
        { "BS-RC", (10, "Rum Cay") },
        { "BS-SS", (10, "San Salvador") },
        { "BS-SO", (10, "South Abaco") },
        { "BS-SA", (10, "South Andros") },
        { "BS-SE", (10, "South Eleuthera") },
        { "BS-SW", (10, "Spanish Wells") },
        { "BS-WG", (10, "West Grand Bahama") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BB = new()
    {
        { "BB-01", (10, "Christ Church") },
        { "BB-02", (10, "Saint Andrew") },
        { "BB-03", (10, "Saint George") },
        { "BB-04", (10, "Saint James") },
        { "BB-05", (10, "Saint John") },
        { "BB-06", (10, "Saint Joseph") },
        { "BB-07", (10, "Saint Lucy") },
        { "BB-08", (10, "Saint Michael") },
        { "BB-09", (10, "Saint Peter") },
        { "BB-10", (10, "Saint Philip") },
        { "BB-11", (10, "Saint Thomas") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> TT = new()
    {
        { "TT-ARI", (10, "Arima") },
        { "TT-CHA", (10, "Chaguanas") },
        { "TT-CTT", (10, "Couva-Tabaquite-Talparo") },
        { "TT-DMN", (10, "Diego Martin") },
        { "TT-MRC", (10, "Mayaro-Rio Claro") },
        { "TT-PED", (10, "Penal-Debe") },
        { "TT-POS", (10, "Port of Spain") },
        { "TT-PRT", (10, "Princes Town") },
        { "TT-PTF", (10, "Point Fortin") },
        { "TT-SFO", (10, "San Fernando") },
        { "TT-SGE", (10, "Sangre Grande") },
        { "TT-SIP", (10, "Siparia") },
        { "TT-SJL", (10, "San Juan-Laventille") },
        { "TT-TOB", (10, "Tobago") },
        { "TT-TUP", (10, "Tunapuna-Piarco") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> VC = new()
    {
        { "VC-01", (10, "Charlotte") },
        { "VC-06", (10, "Grenadines") },
        { "VC-02", (10, "Saint Andrew") },
        { "VC-03", (10, "Saint David") },
        { "VC-04", (10, "Saint George") },
        { "VC-05", (10, "Saint Patrick") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GD = new()
    {
        { "GD-01", (10, "Saint Andrew") },
        { "GD-02", (10, "Saint David") },
        { "GD-03", (10, "Saint George") },
        { "GD-04", (10, "Saint John") },
        { "GD-05", (10, "Saint Mark") },
        { "GD-06", (10, "Saint Patrick") },
        { "GD-10", (10, "Southern Grenadine Islands") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> DM = new()
    {
        { "DM-02", (10, "Saint Andrew") },
        { "DM-03", (10, "Saint David") },
        { "DM-04", (10, "Saint George") },
        { "DM-05", (10, "Saint John") },
        { "DM-06", (10, "Saint Joseph") },
        { "DM-07", (10, "Saint Luke") },
        { "DM-08", (10, "Saint Mark") },
        { "DM-09", (10, "Saint Patrick") },
        { "DM-10", (10, "Saint Paul") },
        { "DM-11", (10, "Saint Peter") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> KN = new()
    {
        { "KN-K", (10, "Saint Kitts") },
        { "KN-N", (10, "Nevis") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> SR = new()
    {
        { "SR-BR", (10, "Brokopondo") },
        { "SR-CM", (10, "Commewijne") },
        { "SR-CR", (10, "Coronie") },
        { "SR-MA", (10, "Marowijne") },
        { "SR-NI", (10, "Nickerie") },
        { "SR-PR", (10, "Para") },
        { "SR-PM", (10, "Paramaribo") },
        { "SR-SA", (10, "Saramacca") },
        { "SR-SI", (10, "Sipaliwini") },
        { "SR-WA", (10, "Wanica") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GY = new()
    {
        { "GY-BA", (10, "Barima-Waini") },
        { "GY-CU", (10, "Cuyuni-Mazaruni") },
        { "GY-DE", (10, "Demerara-Mahaica") },
        { "GY-EB", (10, "East Berbice-Corentyne") },
        { "GY-ES", (10, "Essequibo Islands-West Demerara") },
        { "GY-MA", (10, "Mahaica-Berbice") },
        { "GY-PM", (10, "Pomeroon-Supenaam") },
        { "GY-PT", (10, "Potaro-Siparuni") },
        { "GY-UD", (10, "Upper Demerara-Berbice") },
        { "GY-UT", (10, "Upper Takutu-Upper Essequibo") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> VE = new()
    {
        { "VE-Z", (10, "Amazonia") },
        { "VE-B", (10, "Anzoategui") },
        { "VE-C", (10, "Apure") },
        { "VE-D", (10, "Aragua") },
        { "VE-E", (10, "Barinas") },
        { "VE-F", (10, "Bolivar") },
        { "VE-G", (10, "Carabobo") },
        { "VE-H", (10, "Cojedes") },
        { "VE-Y", (10, "Amacuro Delta") },
        { "VE-W", (10, "Federal Dependencies") },
        { "VE-A", (10, "Caracas") },
        { "VE-I", (10, "Falcon") },
        { "VE-J", (10, "Guarico") },
        { "VE-X", (10, "La Guaira") },
        { "VE-K", (10, "Lara") },
        { "VE-L", (10, "Merida") },
        { "VE-M", (10, "Miranda") },
        { "VE-N", (10, "Monagas") },
        { "VE-O", (10, "New Sparta") },
        { "VE-P", (10, "Portuguesa") },
        { "VE-R", (10, "Sucre") },
        { "VE-S", (10, "Tachira") },
        { "VE-T", (10, "Trujillo") },
        { "VE-U", (10, "Yaracuy") },
        { "VE-V", (10, "Zulia ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> GE = new()
    {
        { "GE-AB", (10, "Abkhazia") },
        { "GE-AJ", (10, "Ajaria") },
        { "GE-GU", (10, "Guria") },
        { "GE-IM", (10, "Imereti") },
        { "GE-KA", (10, "K'akheti") },
        { "GE-KK", (10, "Kvemo Kartli") },
        { "GE-MM", (10, "Mtskheta-Mtianeti") },
        { "GE-RL", (10, "Rach'a-Lechkhumi-Kvemo Svaneti") },
        { "GE-SZ", (10, "Samegrelo-Zemo Svaneti") },
        { "GE-SJ", (10, "Samtskhe-Javakheti") },
        { "GE-SK", (10, "Shida Kartli") },
        { "GE-TB", (10, "Tbilisi") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AM = new()
    {
        { "AM-AG", (10, "Aragatsotn") },
        { "AM-AR", (10, "Ararat") },
        { "AM-AV", (10, "Armavir") },
        { "AM-ER", (10, "Yerevan") },
        { "AM-GR", (10, "Gegharkunik") },
        { "AM-KT", (10, "Kotayk") },
        { "AM-LO", (10, "Lori") },
        { "AM-SH", (10, "Shirak") },
        { "AM-SU", (10, "Syunik") },
        { "AM-TV", (10, "Tavush") },
        { "AM-VD", (10, "Vayots Dzor") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> AZ = new()
    {
        { "AZ-ABS", (10, "Absheron") },
        { "AZ-AGC", (10, "Aghjabadi") },
        { "AZ-AGM", (10, "Aghdam") },
        { "AZ-AGS", (10, "Agdash") },
        { "AZ-AGA", (10, "Aghstafa") },
        { "AZ-AGU", (10, "Aghsu") },
        { "AZ-AST", (10, "Astara") },
        { "AZ-BAB", (10, "Babek") },
        { "AZ-BA", (10, "Baku") },
        { "AZ-BAL", (10, "Balakan") },
        { "AZ-BAR", (10, "Barda") },
        { "AZ-BEY", (10, "Beylagan") },
        { "AZ-BIL", (10, "Bilasuvar") },
        { "AZ-CAB", (10, "Jabrayil") },
        { "AZ-CAL", (10, "Jalilabad") },
        { "AZ-CUL", (10, "Julfa") },
        { "AZ-DAS", (10, "Dashkasan") },
        { "AZ-FUZ", (10, "Fuzuli") },
        { "AZ-GAD", (10, "Gadabay") },
        { "AZ-GA", (10, "Ganja") },
        { "AZ-GOR", (10, "Goranboy") },
        { "AZ-GOY", (10, "Goychay") },
        { "AZ-GYG", (10, "Goygol") },
        { "AZ-HAC", (10, "Hajigabul") },
        { "AZ-IMI", (10, "Imishli") },
        { "AZ-ISM", (10, "Ismayilli") },
        { "AZ-KAL", (10, "Kalbajar") },
        { "AZ-KAN", (10, "Kangarli") },
        { "AZ-KUR", (10, "Kurdamir") },
        { "AZ-LAC", (10, "Lachin") },
        { "AZ-LA", (10, "Lankaran City") },
        { "AZ-LAN", (10, "Lankaran") },
        { "AZ-LER", (10, "Lerik") },
        { "AZ-MAS", (10, "Masally") },
        { "AZ-MI", (10, "Mingachevir") },
        { "AZ-NA", (10, "Naftalan") },
        { "AZ-NV", (10, "Nakhchivan City") },
        { "AZ-NEF", (10, "Neftchala") },
        { "AZ-OGU", (10, "Oghuz") },
        { "AZ-ORD", (10, "Ordubad") },
        { "AZ-QAX", (10, "Gakh") },
        { "AZ-QAZ", (10, "Gazakh") },
        { "AZ-QAB", (10, "Gabala") },
        { "AZ-QOB", (10, "Gobustan") },
        { "AZ-QBA", (10, "Guba") },
        { "AZ-QBI", (10, "Gubadlı") },
        { "AZ-QUS", (10, "Gusar") },
        { "AZ-SAT", (10, "Saatly") },
        { "AZ-SAB", (10, "Sabirabad") },
        { "AZ-SBN", (10, "Shabran") },
        { "AZ-SAH", (10, "Shahbuz") },
        { "AZ-SAL", (10, "Salyan") },
        { "AZ-SMI", (10, "Shamakhi") },
        { "AZ-SMX", (10, "Samukh") },
        { "AZ-SAD", (10, "Sadarak") },
        { "AZ-SA", (10, "Shaki City") },
        { "AZ-SAK", (10, "Shaki") },
        { "AZ-SKR", (10, "Shamkir") },
        { "AZ-SAR", (10, "Sharur") },
        { "AZ-SR", (10, "Shirvan") },
        { "AZ-SIY", (10, "Siyazan") },
        { "AZ-SM", (10, "Sumgayit") },
        { "AZ-SUS", (10, "Shusha") },
        { "AZ-TAR", (10, "Tartar") },
        { "AZ-TOV", (10, "Tovuz") },
        { "AZ-UCA", (10, "Ujar") },
        { "AZ-XAC", (10, "Khachmaz") },
        { "AZ-XA", (10, "Khankendi") },
        { "AZ-XIZ", (10, "Khizi") },
        { "AZ-XCI", (10, "Khojaly") },
        { "AZ-XVD", (10, "Khojavend") },
        { "AZ-YAR", (10, "Yardimli") },
        { "AZ-YE", (10, "Yevlakh City") },
        { "AZ-YEV", (10, "Yevlakh") },
        { "AZ-ZAQ", (10, "Zagatala") },
        { "AZ-ZAN", (10, "Zangilan") },
        { "AZ-ZAR", (10, "Zardab ") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> VU = new()
    {
        { "VU-MAP", (10, "Malampa") },
        { "VU-PAM", (10, "Pénama") },
        { "VU-SAM", (10, "Sanma") },
        { "VU-SEE", (10, "Shéfa") },
        { "VU-TAE", (10, "Taféa") },
        { "VU-TOB", (10, "Torba") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> BY = new()
    {
        { "BY-BR", (0, "Brestskaja oblast'") },
        { "BY-HO", (0, "Gomel'skaja oblast'") },
        { "BY-HM", (1, "Gorod Minsk") },
        { "BY-HR", (0, "Grodnenskaja oblast'") },
        { "BY-MA", (0, "Mogilevskaja oblast'") },
        { "BY-MI", (0, "Minskaja oblast'") },
        { "BY-VI", (0, "Vitebskaja oblast'") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> FJ = new()
    {
        { "FJ-C", (10, "Central") },
        { "FJ-E", (10, "Eastern") },
        { "FJ-N", (10, "Northern") },
        { "FJ-W", (10, "Western") },
        { "FJ-R", (10, "Rotuma") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> NC = new()
    {
        { "NC-1", (10, "New Caledonia") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> TO = new()
    {
        { "TO-01", (10, "'Eua") },
        { "TO-02", (10, "Ha'apai") },
        { "TO-03", (10, "Niuas") },
        { "TO-04", (10, "Tongatapu") },
        { "TO-05", (10, "Vava'u") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> TV = new()
    {
        { "TV-FUN", (10, "Funafuti") },
        { "TV-NMG", (10, "Nanumaga") },
        { "TV-NMA", (10, "Nanumea") },
        { "TV-NIT", (10, "Niutao") },
        { "TV-NUI", (10, "Nui") },
        { "TV-NKF", (10, "Nukufetau") },
        { "TV-NKL", (10, "Nukulaelae") },
        { "TV-VAI", (10, "Vaitupu") },
    };

    private static readonly Dictionary<string, (int weight, string subdivisionName)> CY = new()
    {
        { "CY-04", (10, "Famagusta") },
        { "CY-06", (10, "Kyrenia") },
        { "CY-03", (10, "Larnaca") },
        { "CY-01", (10, "Nicosia") },
        { "CY-02", (10, "Limassol") },
        { "CY-05", (10, "Paphos") },
    };

    public static readonly Dictionary<string, Dictionary<string, (int weight, string subdivisionName)>> CountryToSubdivision =
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
            { "AX", AX },
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
            { "LI", LI },
            { "OM", OM },
        };

    public static readonly Dictionary<string, Dictionary<string, (int weight, string subdivisionName)>> NotQuiteThereYetCountryToSubdivision =
        new()
        {
            { "NA", NA },
            { "PY", PY },
            { "BA", BA },
            { "VN", VN },
            { "NP", NP },
            { "UZ", UZ },
            { "TJ", TJ },
            { "SA", SA },
            { "BH", BH },
            { "HN", HN },
            { "MA", MA },
            { "KW", KW },
            { "SV", SV },
            { "NI", NI },
            { "BZ", BZ },
            { "JM", JM },
            { "BS", BS },
            { "BB", BB },
            { "TT", TT },
            { "VC", VC },
            { "GD", GD },
            { "DM", DM },
            { "KN", KN },
            { "SR", SR },
            { "GY", GY },
            { "VE", VE },
            { "GE", GE },
            { "AM", AM },
            { "AZ", AZ },
            { "VU", VU },
            { "FJ", FJ },
            { "NC", NC },
            { "TO", TO },
            { "TV", TV },
            { "CY", CY },
        };

    private static Dictionary<string, SubdivisionInfo[]>? _subdivisions;

    public static int GoalForSubdivision(string countryCode, string subdivisionCode, int totalGoalCount, string[]? availableSubdivisions = null)
    {
        var regionTotalWeight = CountryToSubdivision.TryGetValue(countryCode, out var weights)
            ? weights.Where(w => availableSubdivisions == null || availableSubdivisions.Any(a => a == w.Key)).Sum(x => x.Value.weight)
            : (int?)null;

        if (regionTotalWeight == null)
        {
            throw new InvalidOperationException($"Weight for subdivision {subdivisionCode} is null.");
        }

        if (weights == null || !weights.TryGetValue(subdivisionCode, out var value))
        {
            throw new InvalidOperationException($"Subdivision code {subdivisionCode} is not defined.");
        }

        var regionGoalCount = ((decimal)value.weight / regionTotalWeight * totalGoalCount).Value.RoundToInt();
        return regionGoalCount;
    }

    public static string SubdivisionName(string countryCode, string code)
    {
        var subdivisions = SubdivisionWeights.CountryToSubdivision.TryGetValue(countryCode, out var value)
            ? value
            : SubdivisionWeights.NotQuiteThereYetCountryToSubdivision[countryCode];
        return subdivisions.TryGetValue(code, out var val)
            ? val.subdivisionName
            : "N/A";
    }


    public static string SubdivisionNameBySubdivisionCode(string code)
    {
        return SubdivisionWeights.CountryToSubdivision.FirstOrDefault(x => x.Value.Any(y => y.Key == code)).Value?
            .First(x => x.Key == code).Value.subdivisionName ?? "N/A";
    }

    public static (string subdivisionCode, string file)[] AllSubdivisionFiles(string countryCode, RunMode runMode)
    {
        var subdivisionKeys = CountryToSubdivision[countryCode].Where(x => x.Value.weight > 0).Select(x => x.Key);
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