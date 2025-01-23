using System.Text.RegularExpressions;

namespace FileFlows.Plugin;

/// <summary>
/// Helper to lookup language names/codes
/// </summary>
public class LanguageHelper
{
    public static readonly LanguageDefintion[] Languages;
 
    static LanguageHelper()
    {
        Languages = new LanguageDefintion[]
        {
            new () {English = "Afar",French = "afar",German = "Danakil-Sprache",Iso2 = "aar",Iso1 = "aa"},
            new () {English = "Abkhazian",French = "abkhaze",German = "Abchasisch",Iso2 = "abk",Iso1 = "ab"},
            new () {English = "Achinese",French = "aceh",German = "Aceh-Sprache",Iso2 = "ace"},
            new () {English = "Acoli",French = "acoli",German = "Acholi-Sprache",Iso2 = "ach"},
            new () {English = "Adangme",French = "adangme",German = "Adangme-Sprache",Iso2 = "ada"},
            new () {English = "Adyghe",French = "adygh\u00E9",German = "Adygisch",Iso2 = "ady"},
            new () {English = "Afro-Asiatic languages",French = "afro-asiatiques, langues",German = "Hamitosemitische Sprachen (Andere)",Iso2 = "afa"},
            new () {English = "Afrihili",French = "afrihili",German = "Afrihili",Iso2 = "afh"},
            new () {English = "Afrikaans",French = "afrikaans",German = "Afrikaans",Iso2 = "afr",Iso1 = "af"},
            new () {English = "Ainu",French = "a\u00EFnou",German = "Ainu-Sprache",Iso2 = "ain"},
            new () {English = "Akan",French = "akan",German = "Akan-Sprache",Iso2 = "aka",Iso1 = "ak"},
            new () {English = "Akkadian",French = "akkadien",German = "Akkadisch",Iso2 = "akk"},
            new () {English = "Albanian",French = "albanais",German = "Albanisch",Iso2 = "sqi",Iso1 = "sq"},
            new () {English = "Aleut",French = "al\u00E9oute",German = "Aleutisch",Iso2 = "ale"},
            new () {English = "Algonquian languages",French = "algonquines, langues",German = "Algonkin-Sprachen (Andere)",Iso2 = "alg"},
            new () {English = "Southern Altai",French = "altai du Sud",German = "Altaisch",Iso2 = "alt"},
            new () {English = "Amharic",French = "amharique",German = "Amharisch",Iso2 = "amh",Iso1 = "am"},
            new () {English = "Angika",French = "angika",German = "Anga-Sprache",Iso2 = "anp"},
            new () {English = "Apache languages",French = "apaches, langues",German = "Apachen-Sprachen",Iso2 = "apa"},
            new () {English = "Arabic",French = "arabe",German = "Arabisch",Iso2 = "ara",Iso1 = "ar"},
            new () {English = "Official Aramaic (700-300 BCE)",French = "aram\u00E9en d\u0027empire (700-300 BCE)",German = "Aram\u00E4isch",Iso2 = "arc"},
            new () {English = "Aragonese",French = "aragonais",German = "Aragonesisch",Iso2 = "arg",Iso1 = "an"},
            new () {English = "Armenian",French = "arm\u00E9nien",German = "Armenisch",Iso2 = "hye",Iso1 = "hy"},
            new () {English = "Mapudungun",French = "mapudungun",German = "Arauka-Sprachen",Iso2 = "arn"},
            new () {English = "Arapaho",French = "arapaho",German = "Arapaho-Sprache",Iso2 = "arp"},
            new () {English = "Artificial languages",French = "artificielles, langues",German = "Kunstsprachen (Andere)",Iso2 = "art"},
            new () {English = "Arawak",French = "arawak",German = "Arawak-Sprachen",Iso2 = "arw"},
            new () {English = "Assamese",French = "assamais",German = "Assamesisch",Iso2 = "asm",Iso1 = "as"},
            new () {English = "Asturian",French = "asturien",German = "Asturisch",Iso2 = "ast"},
            new () {English = "Athapascan languages",French = "athapascanes, langues",German = "Athapaskische Sprachen (Andere)",Iso2 = "ath"},
            new () {English = "Australian languages",French = "australiennes, langues",German = "Australische Sprachen",Iso2 = "aus"},
            new () {English = "Avaric",French = "avar",German = "Awarisch",Iso2 = "ava",Iso1 = "av"},
            new () {English = "Avestan",French = "avestique",German = "Avestisch",Iso2 = "ave",Iso1 = "ae"},
            new () {English = "Awadhi",French = "awadhi",German = "Awadhi",Iso2 = "awa"},
            new () {English = "Aymara",French = "aymara",German = "Aymar\u00E1-Sprache",Iso2 = "aym",Iso1 = "ay"},
            new () {English = "Azerbaijani",French = "az\u00E9ri",German = "Aserbeidschanisch",Iso2 = "aze",Iso1 = "az"},
            new () {English = "Banda languages",French = "banda, langues",German = "Banda-Sprachen (Ubangi-Sprachen)",Iso2 = "bad"},
            new () {English = "Bamileke languages",French = "bamil\u00E9k\u00E9, langues",German = "Bamileke-Sprachen",Iso2 = "bai"},
            new () {English = "Bashkir",French = "bachkir",German = "Baschkirisch",Iso2 = "bak",Iso1 = "ba"},
            new () {English = "Baluchi",French = "baloutchi",German = "Belutschisch",Iso2 = "bal"},
            new () {English = "Bambara",French = "bambara",German = "Bambara-Sprache",Iso2 = "bam",Iso1 = "bm"},
            new () {English = "Balinese",French = "balinais",German = "Balinesisch",Iso2 = "ban"},
            new () {English = "Basque",French = "basque",German = "Baskisch",Iso2 = "eus",Iso1 = "eu"},
            new () {English = "Basa",French = "basa",German = "Basaa-Sprache",Iso2 = "bas"},
            new () {English = "Baltic languages",French = "baltes, langues",German = "Baltische Sprachen (Andere)",Iso2 = "bat"},
            new () {English = "Beja",French = "bedja",German = "Bedauye",Iso2 = "bej"},
            new () {English = "Belarusian",French = "bi\u00E9lorusse",German = "Wei\u00DFrussisch",Iso2 = "bel",Iso1 = "be"},
            new () {English = "Bemba",French = "bemba",German = "Bemba-Sprache",Iso2 = "bem"},
            new () {English = "Bengali",French = "bengali",German = "Bengali",Iso2 = "ben",Iso1 = "bn"},
            new () {English = "Berber languages",French = "berb\u00E8res, langues",German = "Berbersprachen (Andere)",Iso2 = "ber"},
            new () {English = "Bhojpuri",French = "bhojpuri",German = "Bhojpuri",Iso2 = "bho"},
            new () {English = "Bihari languages",French = "langues biharis",German = "Bihari (Andere)",Iso2 = "bih",Iso1 = "bh"},
            new () {English = "Bikol",French = "bikol",German = "Bikol-Sprache",Iso2 = "bik"},
            new () {English = "Bini",French = "bini",German = "Edo-Sprache",Iso2 = "bin"},
            new () {English = "Bislama",French = "bichlamar",German = "Beach-la-mar",Iso2 = "bis",Iso1 = "bi"},
            new () {English = "Siksika",French = "blackfoot",German = "Blackfoot-Sprache",Iso2 = "bla"},
            new () {English = "Bantu languages",French = "bantou, langues",German = "Bantusprachen (Andere)",Iso2 = "bnt"},
            new () {English = "Tibetan",French = "tib\u00E9tain",German = "Tibetisch",Iso2 = "bod",Iso1 = "bo"},
            new () {English = "Bosnian",French = "bosniaque",German = "Bosnisch",Iso2 = "bos",Iso1 = "bs"},
            new () {English = "Braj",French = "braj",German = "Braj-Bhakha",Iso2 = "bra"},
            new () {English = "Breton",French = "breton",German = "Bretonisch",Iso2 = "bre",Iso1 = "br"},
            new () {English = "Batak languages",French = "batak, langues",German = "Batak-Sprache",Iso2 = "btk"},
            new () {English = "Buriat",French = "bouriate",German = "Burjatisch",Iso2 = "bua"},
            new () {English = "Buginese",French = "bugi",German = "Bugi-Sprache",Iso2 = "bug"},
            new () {English = "Bulgarian",French = "bulgare",German = "Bulgarisch",Iso2 = "bul",Iso1 = "bg"},
            new () {English = "Burmese",French = "birman",German = "Birmanisch",Iso2 = "mya",Iso1 = "my"},
            new () {English = "Blin",French = "blin",German = "Bilin-Sprache",Iso2 = "byn"},
            new () {English = "Caddo",French = "caddo",German = "Caddo-Sprachen",Iso2 = "cad"},
            new () {English = "Central American Indian languages",French = "am\u00E9rindiennes de l\u0027Am\u00E9rique centrale,  langues",German = "Indianersprachen, Zentralamerika (Andere)",Iso2 = "cai"},
            new () {English = "Galibi Carib",French = "karib",German = "Karibische Sprachen",Iso2 = "car"},
            new () {English = "Catalan",French = "catalan",German = "Katalanisch",Iso2 = "cat",Iso1 = "ca"},
            new () {English = "Caucasian languages",French = "caucasiennes, langues",German = "Kaukasische Sprachen (Andere)",Iso2 = "cau"},
            new () {English = "Cebuano",French = "cebuano",German = "Cebuano",Iso2 = "ceb"},
            new () {English = "Celtic languages",French = "celtiques, langues",German = "Keltische Sprachen (Andere)",Iso2 = "cel"},
            new () {English = "Czech",French = "tch\u00E8que",German = "Tschechisch",Iso2 = "ces",Iso1 = "cs"},
            new () {English = "Chamorro",French = "chamorro",German = "Chamorro-Sprache",Iso2 = "cha",Iso1 = "ch"},
            new () {English = "Chibcha",French = "chibcha",German = "Chibcha-Sprachen",Iso2 = "chb"},
            new () {English = "Chechen",French = "tch\u00E9tch\u00E8ne",German = "Tschetschenisch",Iso2 = "che",Iso1 = "ce"},
            new () {English = "Chagatai",French = "djaghata\u00EF",German = "Tschagataisch",Iso2 = "chg"},
            new () {English = "Chinese",French = "chinois", German = "Chinesisch",Iso2 = "zho",Iso1 = "zh", Aliases = ["chi"], NativeName = "中文" },
            new () {English = "Chinese Traditional", French = "chinois traditionnel", German = "Traditionelles Chinesisch", Iso2 = "zht", Iso1 = "zh-Hant", Aliases = ["cht"], NativeName = "繁體中文" },
            new () {English = "Chuukese",French = "chuuk",German = "Trukesisch",Iso2 = "chk"},
            new () {English = "Mari",French = "mari",German = "Tscheremissisch",Iso2 = "chm"},
            new () {English = "Chinook jargon",French = "chinook, jargon",German = "Chinook-Jargon",Iso2 = "chn"},
            new () {English = "Choctaw",French = "choctaw",German = "Choctaw-Sprache",Iso2 = "cho"},
            new () {English = "Chipewyan",French = "chipewyan",German = "Chipewyan-Sprache",Iso2 = "chp"},
            new () {English = "Cherokee",French = "cherokee",German = "Cherokee-Sprache",Iso2 = "chr"},
            new () {English = "Church Slavic",French = "slavon d\u0027\u00E9glise",German = "Kirchenslawisch",Iso2 = "chu",Iso1 = "cu"},
            new () {English = "Chuvash",French = "tchouvache",German = "Tschuwaschisch",Iso2 = "chv",Iso1 = "cv"},
            new () {English = "Cheyenne",French = "cheyenne",German = "Cheyenne-Sprache",Iso2 = "chy"},
            new () {English = "Chamic languages",French = "chames, langues",German = "Cham-Sprachen",Iso2 = "cmc"},
            new () {English = "Coptic",French = "copte",German = "Koptisch",Iso2 = "cop"},
            new () {English = "Cornish",French = "cornique",German = "Kornisch",Iso2 = "cor",Iso1 = "kw"},
            new () {English = "Corsican",French = "corse",German = "Korsisch",Iso2 = "cos",Iso1 = "co"},
            new () {English = "Cree",French = "cree",German = "Cree-Sprache",Iso2 = "cre",Iso1 = "cr"},
            new () {English = "Crimean Tatar",French = "tatar de Crim\u00E9",German = "Krimtatarisch",Iso2 = "crh"},
            new () {English = "Creoles and pidgins",French = "cr\u00E9oles et pidgins",German = "Kreolische Sprachen",Iso2 = "crp"},
            new () {English = "Kashubian",French = "kachoube",German = "Kaschubisch",Iso2 = "csb"},
            new () {English = "Cushitic languages",French = "couchitiques,  langues",German = "Kuschitische Sprachen (Andere)",Iso2 = "cus"},
            new () {English = "Welsh",French = "gallois",German = "Kymrisch",Iso2 = "cym",Iso1 = "cy"},
            new () {English = "Czech",French = "tch\u00E8que",German = "Tschechisch",Iso2 = "ces",Iso1 = "cs"},
            new () {English = "Dakota",French = "dakota",German = "Dakota-Sprache",Iso2 = "dak"},
            new () {English = "Danish",French = "danois",German = "D\u00E4nisch",Iso2 = "dan",Iso1 = "da"},
            new () {English = "Dargwa",French = "dargwa",German = "Darginisch",Iso2 = "dar"},
            new () {English = "Land Dayak languages",French = "dayak, langues",German = "Dajakisch",Iso2 = "day"},
            new () {English = "Delaware",French = "delaware",German = "Delaware-Sprache",Iso2 = "del"},
            new () {English = "Slave (Athapascan)",French = "esclave (athapascan)",German = "Slave-Sprache",Iso2 = "den"},
            new () {English = "German",French = "allemand",German = "Deutsch", Iso2 = "deu", Iso1 = "de", Aliases = [ "ger" ], NativeName = "Deutsch"},
            new () {English = "Dogrib",French = "dogrib",German = "Dogrib-Sprache",Iso2 = "dgr"},
            new () {English = "Dinka",French = "dinka",German = "Dinka-Sprache",Iso2 = "din"},
            new () {English = "Divehi",French = "maldivien",German = "Maledivisch",Iso2 = "div",Iso1 = "dv"},
            new () {English = "Dogri",French = "dogri",German = "Dogri",Iso2 = "doi"},
            new () {English = "Dravidian languages",French = "dravidiennes,  langues",German = "Drawidische Sprachen (Andere)",Iso2 = "dra"},
            new () {English = "Lower Sorbian",French = "bas-sorabe",German = "Niedersorbisch",Iso2 = "dsb"},
            new () {English = "Duala",French = "douala",German = "Duala-Sprachen",Iso2 = "dua"},
            new () {English = "Dutch",French = "n\u00E9erlandais",German = "Niederl\u00E4ndisch",Iso2 = "nld",Iso1 = "nl", Aliases = ["dut"],NativeName = "Nederlands"},
            new () {English = "Dyula",French = "dioula",German = "Dyula-Sprache",Iso2 = "dyu"},
            new () {English = "Dzongkha",French = "dzongkha",German = "Dzongkha",Iso2 = "dzo",Iso1 = "dz"},
            new () {English = "Efik",French = "efik",German = "Efik",Iso2 = "efi"},
            new () {English = "Egyptian (Ancient)",French = "\u00E9gyptien",German = "\u00C4gyptisch",Iso2 = "egy"},
            new () {English = "Ekajuk",French = "ekajuk",German = "Ekajuk",Iso2 = "eka"},
            new () {English = "Greek, Modern (1453-)",French = "grec moderne (apr\u00E8s 1453)",German = "Neugriechisch",Iso2 = "ell",Iso1 = "el"},
            new () {English = "Elamite",French = "\u00E9lamite",German = "Elamisch",Iso2 = "elx"},
            new () {English = "English",French = "anglais",German = "Englisch",Iso2 = "eng",Iso1 = "en" , NativeName = "English"},
            new () {English = "Esperanto",French = "esp\u00E9ranto",German = "Esperanto",Iso2 = "epo",Iso1 = "eo"},
            new () {English = "Estonian",French = "estonien",German = "Estnisch",Iso2 = "est",Iso1 = "et"},
            new () {English = "Basque",French = "basque",German = "Baskisch",Iso2 = "eus",Iso1 = "eu"},
            new () {English = "Ewe",French = "\u00E9w\u00E9",German = "Ewe-Sprache",Iso2 = "ewe",Iso1 = "ee"},
            new () {English = "Ewondo",French = "\u00E9wondo",German = "Ewondo",Iso2 = "ewo"},
            new () {English = "Fang",French = "fang",German = "Pangwe-Sprache",Iso2 = "fan"},
            new () {English = "Faroese",French = "f\u00E9ro\u00EFen",German = "F\u00E4r\u00F6isch",Iso2 = "fao",Iso1 = "fo"},
            new () {English = "Persian",French = "persan",German = "Persisch",Iso2 = "fas",Iso1 = "fa"},
            new () {English = "Fanti",French = "fanti",German = "Fante-Sprache",Iso2 = "fat"},
            new () {English = "Fijian",French = "fidjien",German = "Fidschi-Sprache",Iso2 = "fij",Iso1 = "fj"},
            new () {English = "Filipino",French = "filipino",German = "Pilipino",Iso2 = "fil"},
            new () {English = "Finnish",French = "finnois",German = "Finnisch",Iso2 = "fin",Iso1 = "fi"},
            new () {English = "Finno-Ugrian languages",French = "finno-ougriennes,  langues",German = "Finnougrische Sprachen (Andere)",Iso2 = "fiu"},
            new () {English = "Fon",French = "fon",German = "Fon-Sprache",Iso2 = "fon"},
            new () {English = "French",French = "fran\u00E7ais",German = "Franz\u00F6sisch",Iso2 = "fre",Iso1 = "fr", Aliases = ["fra"], NativeName = "Français"},
            new () {English = "French (Canada)", French = "Français (Canada)", German = "Französisch (Kanada)", Iso2 = "fra-CA", Iso1 = "fr-CA"},
            new () {English = "Northern Frisian",French = "frison septentrional",German = "Nordfriesisch",Iso2 = "frr"},
            new () {English = "Eastern Frisian",French = "frison oriental",German = "Ostfriesisch",Iso2 = "frs"},
            new () {English = "Western Frisian",French = "frison occidental",German = "Friesisch",Iso2 = "fry",Iso1 = "fy"},
            new () {English = "Fulah",French = "peul",German = "Ful",Iso2 = "ful",Iso1 = "ff"},
            new () {English = "Friulian",French = "frioulan",German = "Friulisch",Iso2 = "fur"},
            new () {English = "Ga",French = "ga",German = "Ga-Sprache",Iso2 = "gaa"},
            new () {English = "Gayo",French = "gayo",German = "Gayo-Sprache",Iso2 = "gay"},
            new () {English = "Gbaya",French = "gbaya",German = "Gbaya-Sprache",Iso2 = "gba"},
            new () {English = "Georgian",French = "g\u00E9orgien",German = "Georgisch",Iso2 = "kat",Iso1 = "ka"},
            new () {English = "Geez",French = "gu\u00E8ze",German = "Alt\u00E4thiopisch",Iso2 = "gez"},
            new () {English = "Gilbertese",French = "kiribati",German = "Gilbertesisch",Iso2 = "gil"},
            new () {English = "Gaelic",French = "ga\u00E9lique",German = "G\u00E4lisch-Schottisch",Iso2 = "gla",Iso1 = "gd"},
            new () {English = "Irish",French = "irlandais",German = "Irisch",Iso2 = "gle",Iso1 = "ga"},
            new () {English = "Galician",French = "galicien",German = "Galicisch",Iso2 = "glg",Iso1 = "gl"},
            new () {English = "Manx",French = "manx",German = "Manx",Iso2 = "glv",Iso1 = "gv"},
            new () {English = "Gondi",French = "gond",German = "Gondi-Sprache",Iso2 = "gon"},
            new () {English = "Gorontalo",French = "gorontalo",German = "Gorontalesisch",Iso2 = "gor"},
            new () {English = "Gothic",French = "gothique",German = "Gotisch",Iso2 = "got"},
            new () {English = "Grebo",French = "grebo",German = "Grebo-Sprache",Iso2 = "grb"},
            new () {English = "Greek, Ancient (to 1453)",French = "grec ancien (jusqu\u0027\u00E0 1453)",German = "Griechisch",Iso2 = "grc"},
            new () {English = "Greek, Modern (1453-)",French = "grec moderne (apr\u00E8s 1453)",German = "Neugriechisch",Iso2 = "ell",Iso1 = "el"},
            new () {English = "Guarani",French = "guarani",German = "Guaran\u00ED-Sprache",Iso2 = "grn",Iso1 = "gn"},
            new () {English = "Swiss German",French = "suisse al\u00E9manique",German = "Schweizerdeutsch",Iso2 = "gsw"},
            new () {English = "Gujarati",French = "goudjrati",German = "Gujarati-Sprache",Iso2 = "guj",Iso1 = "gu"},
            new () {English = "Gwich\u0027in",French = "gwich\u0027in",German = "Kutchin-Sprache",Iso2 = "gwi"},
            new () {English = "Haida",French = "haida",German = "Haida-Sprache",Iso2 = "hai"},
            new () {English = "Haitian",French = "ha\u00EFtien",German = "Ha\u00EFtien (Haiti-Kreolisch)",Iso2 = "hat",Iso1 = "ht"},
            new () {English = "Hausa",French = "haoussa",German = "Haussa-Sprache",Iso2 = "hau",Iso1 = "ha"},
            new () {English = "Hawaiian",French = "hawa\u00EFen",German = "Hawaiisch",Iso2 = "haw"},
            new () {English = "Hebrew",French = "h\u00E9breu",German = "Hebr\u00E4isch",Iso2 = "heb",Iso1 = "he"},
            new () {English = "Herero",French = "herero",German = "Herero-Sprache",Iso2 = "her",Iso1 = "hz"},
            new () {English = "Hiligaynon",French = "hiligaynon",German = "Hiligaynon-Sprache",Iso2 = "hil"},
            new () {English = "Himachali languages",French = "langues himachalis",German = "Himachali",Iso2 = "him"},
            new () {English = "Hindi",French = "hindi",German = "Hindi",Iso2 = "hin",Iso1 = "hi"},
            new () {English = "Hittite",French = "hittite",German = "Hethitisch",Iso2 = "hit"},
            new () {English = "Hmong",French = "hmong",German = "Miao-Sprachen",Iso2 = "hmn"},
            new () {English = "Hiri Motu",French = "hiri motu",German = "Hiri-Motu",Iso2 = "hmo",Iso1 = "ho"},
            new () {English = "Croatian",French = "croate",German = "Kroatisch",Iso2 = "hrv",Iso1 = "hr"},
            new () {English = "Upper Sorbian",French = "haut-sorabe",German = "Obersorbisch",Iso2 = "hsb"},
            new () {English = "Hungarian",French = "hongrois",German = "Ungarisch",Iso2 = "hun",Iso1 = "hu"},
            new () {English = "Hupa",French = "hupa",German = "Hupa-Sprache",Iso2 = "hup"},
            new () {English = "Armenian",French = "arm\u00E9nien",German = "Armenisch",Iso2 = "hye",Iso1 = "hy"},
            new () {English = "Iban",French = "iban",German = "Iban-Sprache",Iso2 = "iba"},
            new () {English = "Igbo",French = "igbo",German = "Ibo-Sprache",Iso2 = "ibo",Iso1 = "ig"},
            new () {English = "Icelandic",French = "islandais",German = "Isl\u00E4ndisch",Iso2 = "isl",Iso1 = "is"},
            new () {English = "Ido",French = "ido",German = "Ido",Iso2 = "ido",Iso1 = "io"},
            new () {English = "Sichuan Yi",French = "yi de Sichuan",German = "Lalo-Sprache",Iso2 = "iii",Iso1 = "ii"},
            new () {English = "Ijo languages",French = "ijo, langues",German = "Ijo-Sprache",Iso2 = "ijo"},
            new () {English = "Inuktitut",French = "inuktitut",German = "Inuktitut",Iso2 = "iku",Iso1 = "iu"},
            new () {English = "Interlingue",French = "interlingue",German = "Interlingue",Iso2 = "ile",Iso1 = "ie"},
            new () {English = "Iloko",French = "ilocano",German = "Ilokano-Sprache",Iso2 = "ilo"},
            new () {English = "Interlingua (International Auxiliary Language Association)",French = "interlingua (langue auxiliaire internationale)",German = "Interlingua",Iso2 = "ina",Iso1 = "ia"},
            new () {English = "Indic languages",French = "indo-aryennes, langues",German = "Indoarische Sprachen (Andere)",Iso2 = "inc"},
            new () {English = "Indonesian",French = "indon\u00E9sien",German = "Bahasa Indonesia",Iso2 = "ind",Iso1 = "id"},
            new () {English = "Indo-European languages",French = "indo-europ\u00E9ennes, langues",German = "IndoGermanische Sprachen (Andere)",Iso2 = "ine"},
            new () {English = "Ingush",French = "ingouche",German = "Inguschisch",Iso2 = "inh"},
            new () {English = "Inupiaq",French = "inupiaq",German = "Inupik",Iso2 = "ipk",Iso1 = "ik"},
            new () {English = "Iranian languages",French = "iraniennes, langues",German = "Iranische Sprachen (Andere)",Iso2 = "ira"},
            new () {English = "Iroquoian languages",French = "iroquoises, langues",German = "Irokesische Sprachen",Iso2 = "iro"},
            new () {English = "Icelandic",French = "islandais",German = "Isl\u00E4ndisch",Iso2 = "isl",Iso1 = "is"},
            new () {English = "Italian",French = "italien",German = "Italienisch",Iso2 = "ita",Iso1 = "it", NativeName = "Italiano"},
            new () {English = "Javanese",French = "javanais",German = "Javanisch",Iso2 = "jav",Iso1 = "jv"},
            new () {English = "Lojban",French = "lojban",German = "Lojban",Iso2 = "jbo"},
            new () {English = "Japanese",French = "japonais",German = "Japanisch",Iso2 = "jpn",Iso1 = "ja", NativeName = "日本語"},
            new () {English = "Judeo-Persian",French = "jud\u00E9o-persan",German = "J\u00FCdisch-Persisch",Iso2 = "jpr"},
            new () {English = "Judeo-Arabic",French = "jud\u00E9o-arabe",German = "J\u00FCdisch-Arabisch",Iso2 = "jrb"},
            new () {English = "Kara-Kalpak",French = "karakalpak",German = "Karakalpakisch",Iso2 = "kaa"},
            new () {English = "Kabyle",French = "kabyle",German = "Kabylisch",Iso2 = "kab"},
            new () {English = "Kachin",French = "kachin",German = "Kachin-Sprache",Iso2 = "kac"},
            new () {English = "Kalaallisut",French = "groenlandais",German = "Gr\u00F6nl\u00E4ndisch",Iso2 = "kal",Iso1 = "kl"},
            new () {English = "Kamba",French = "kamba",German = "Kamba-Sprache",Iso2 = "kam"},
            new () {English = "Kannada",French = "kannada",German = "Kannada",Iso2 = "kan",Iso1 = "kn"},
            new () {English = "Karen languages",French = "karen, langues",German = "Karenisch",Iso2 = "kar"},
            new () {English = "Kashmiri",French = "kashmiri",German = "Kaschmiri",Iso2 = "kas",Iso1 = "ks"},
            new () {English = "Georgian",French = "g\u00E9orgien",German = "Georgisch",Iso2 = "kat",Iso1 = "ka"},
            new () {English = "Kanuri",French = "kanouri",German = "Kanuri-Sprache",Iso2 = "kau",Iso1 = "kr"},
            new () {English = "Kawi",French = "kawi",German = "Kawi",Iso2 = "kaw"},
            new () {English = "Kazakh",French = "kazakh",German = "Kasachisch",Iso2 = "kaz",Iso1 = "kk"},
            new () {English = "Kabardian",French = "kabardien",German = "Kabardinisch",Iso2 = "kbd"},
            new () {English = "Khasi",French = "khasi",German = "Khasi-Sprache",Iso2 = "kha"},
            new () {English = "Khoisan languages",French = "kho\u00EFsan, langues",German = "Khoisan-Sprachen (Andere)",Iso2 = "khi"},
            new () {English = "Central Khmer",French = "khmer central",German = "Kambodschanisch",Iso2 = "khm",Iso1 = "km"},
            new () {English = "Khotanese",French = "khotanais",German = "Sakisch",Iso2 = "kho"},
            new () {English = "Kikuyu",French = "kikuyu",German = "Kikuyu-Sprache",Iso2 = "kik",Iso1 = "ki"},
            new () {English = "Kinyarwanda",French = "rwanda",German = "Rwanda-Sprache",Iso2 = "kin",Iso1 = "rw"},
            new () {English = "Kirghiz",French = "kirghiz",German = "Kirgisisch",Iso2 = "kir",Iso1 = "ky"},
            new () {English = "Kimbundu",French = "kimbundu",German = "Kimbundu-Sprache",Iso2 = "kmb"},
            new () {English = "Konkani",French = "konkani",German = "Konkani",Iso2 = "kok"},
            new () {English = "Komi",French = "kom",German = "Komi-Sprache",Iso2 = "kom",Iso1 = "kv"},
            new () {English = "Kongo",French = "kongo",German = "Kongo-Sprache",Iso2 = "kon",Iso1 = "kg"},
            new () {English = "Korean",French = "cor\u00E9en",German = "Koreanisch",Iso2 = "kor",Iso1 = "ko",NativeName = "한국어"},
            new () {English = "Kosraean",French = "kosrae",German = "Kosraeanisch",Iso2 = "kos"},
            new () {English = "Kpelle",French = "kpell\u00E9",German = "Kpelle-Sprache",Iso2 = "kpe"},
            new () {English = "Karachay-Balkar",French = "karatchai balkar",German = "Karatschaiisch-Balkarisch",Iso2 = "krc"},
            new () {English = "Karelian",French = "car\u00E9lien",German = "Karelisch",Iso2 = "krl"},
            new () {English = "Kru languages",French = "krou, langues",German = "Kru-Sprachen (Andere)",Iso2 = "kro"},
            new () {English = "Kurukh",French = "kurukh",German = "Oraon-Sprache",Iso2 = "kru"},
            new () {English = "Kuanyama",French = "kuanyama",German = "Kwanyama-Sprache",Iso2 = "kua",Iso1 = "kj"},
            new () {English = "Kumyk",French = "koumyk",German = "Kum\u00FCkisch",Iso2 = "kum"},
            new () {English = "Kurdish",French = "kurde",German = "Kurdisch",Iso2 = "kur",Iso1 = "ku"},
            new () {English = "Kutenai",French = "kutenai",German = "Kutenai-Sprache",Iso2 = "kut"},
            new () {English = "Ladino",French = "jud\u00E9o-espagnol",German = "Judenspanisch",Iso2 = "lad"},
            new () {English = "Lahnda",French = "lahnda",German = "Lahnda",Iso2 = "lah"},
            new () {English = "Lamba",French = "lamba",German = "Lamba-Sprache (Bantusprache)",Iso2 = "lam"},
            new () {English = "Lao",French = "lao",German = "Laotisch",Iso2 = "lao",Iso1 = "lo"},
            new () {English = "Latin",French = "latin",German = "Latein",Iso2 = "lat",Iso1 = "la"},
            new () {English = "Latvian",French = "letton",German = "Lettisch",Iso2 = "lav",Iso1 = "lv"},
            new () {English = "Lezghian",French = "lezghien",German = "Lesgisch",Iso2 = "lez"},
            new () {English = "Limburgan",French = "limbourgeois",German = "Limburgisch",Iso2 = "lim",Iso1 = "li"},
            new () {English = "Lingala",French = "lingala",German = "Lingala",Iso2 = "lin",Iso1 = "ln"},
            new () {English = "Lithuanian",French = "lituanien",German = "Litauisch",Iso2 = "lit",Iso1 = "lt"},
            new () {English = "Mongo",French = "mongo",German = "Mongo-Sprache",Iso2 = "lol"},
            new () {English = "Lozi",French = "lozi",German = "Rotse-Sprache",Iso2 = "loz"},
            new () {English = "Luxembourgish",French = "luxembourgeois",German = "Luxemburgisch",Iso2 = "ltz",Iso1 = "lb"},
            new () {English = "Luba-Lulua",French = "luba-lulua",German = "Lulua-Sprache",Iso2 = "lua"},
            new () {English = "Luba-Katanga",French = "luba-katanga",German = "Luba-Katanga-Sprache",Iso2 = "lub",Iso1 = "lu"},
            new () {English = "Ganda",French = "ganda",German = "Ganda-Sprache",Iso2 = "lug",Iso1 = "lg"},
            new () {English = "Luiseno",French = "luiseno",German = "Luise\u00F1o-Sprache",Iso2 = "lui"},
            new () {English = "Lunda",French = "lunda",German = "Lunda-Sprache",Iso2 = "lun"},
            new () {English = "Luo (Kenya and Tanzania)",French = "luo (Kenya et Tanzanie)",German = "Luo-Sprache",Iso2 = "luo"},
            new () {English = "Lushai",French = "lushai",German = "Lushai-Sprache",Iso2 = "lus"},
            new () {English = "Macedonian",French = "mac\u00E9donien",German = "Makedonisch",Iso2 = "mkd",Iso1 = "mk"},
            new () {English = "Madurese",French = "madourais",German = "Maduresisch",Iso2 = "mad"},
            new () {English = "Magahi",French = "magahi",German = "Khotta",Iso2 = "mag"},
            new () {English = "Marshallese",French = "marshall",German = "Marschallesisch",Iso2 = "mah",Iso1 = "mh"},
            new () {English = "Maithili",French = "maithili",German = "Maithili",Iso2 = "mai"},
            new () {English = "Makasar",French = "makassar",German = "Makassarisch",Iso2 = "mak"},
            new () {English = "Malayalam",French = "malayalam",German = "Malayalam",Iso2 = "mal",Iso1 = "ml"},
            new () {English = "Mandingo",French = "mandingue",German = "Malinke-Sprache",Iso2 = "man"},
            new () {English = "Maori",French = "maori",German = "Maori-Sprache",Iso2 = "mri",Iso1 = "mi"},
            new () {English = "Austronesian languages",French = "austron\u00E9siennes, langues",German = "Austronesische Sprachen (Andere)",Iso2 = "map"},
            new () {English = "Marathi",French = "marathe",German = "Marathi",Iso2 = "mar",Iso1 = "mr"},
            new () {English = "Masai",French = "massa\u00EF",German = "Massai-Sprache",Iso2 = "mas"},
            new () {English = "Malay",French = "malais",German = "Malaiisch",Iso2 = "msa",Iso1 = "ms"},
            new () {English = "Moksha",French = "moksa",German = "Mokscha-Sprache",Iso2 = "mdf"},
            new () {English = "Mandar",French = "mandar",German = "Mandaresisch",Iso2 = "mdr"},
            new () {English = "Mende",French = "mend\u00E9",German = "Mende-Sprache",Iso2 = "men"},
            new () {English = "Mi\u0027kmaq",French = "mi\u0027kmaq",German = "Micmac-Sprache",Iso2 = "mic"},
            new () {English = "Minangkabau",French = "minangkabau",German = "Minangkabau-Sprache",Iso2 = "min"},
            new () {English = "Uncoded languages",French = "langues non cod\u00E9es",German = "Einzelne andere Sprachen",Iso2 = "mis"},
            new () {English = "Macedonian",French = "mac\u00E9donien",German = "Makedonisch",Iso2 = "mkd",Iso1 = "mk"},
            new () {English = "Mon-Khmer languages",French = "m\u00F4n-khmer, langues",German = "Mon-Khmer-Sprachen (Andere)",Iso2 = "mkh"},
            new () {English = "Malagasy",French = "malgache",German = "Malagassi-Sprache",Iso2 = "mlg",Iso1 = "mg"},
            new () {English = "Maltese",French = "maltais",German = "Maltesisch",Iso2 = "mlt",Iso1 = "mt"},
            new () {English = "Manchu",French = "mandchou",German = "Mandschurisch",Iso2 = "mnc"},
            new () {English = "Manipuri",French = "manipuri",German = "Meithei-Sprache",Iso2 = "mni"},
            new () {English = "Manobo languages",French = "manobo, langues",German = "Manobo-Sprachen",Iso2 = "mno"},
            new () {English = "Mohawk",French = "mohawk",German = "Mohawk-Sprache",Iso2 = "moh"},
            new () {English = "Mongolian",French = "mongol",German = "Mongolisch",Iso2 = "mon",Iso1 = "mn"},
            new () {English = "Mossi",French = "mor\u00E9",German = "Mossi-Sprache",Iso2 = "mos"},
            new () {English = "Maori",French = "maori",German = "Maori-Sprache",Iso2 = "mri",Iso1 = "mi"},
            new () {English = "Malay",French = "malais",German = "Malaiisch",Iso2 = "msa",Iso1 = "ms"},
            new () {English = "Multiple languages",French = "multilingue",German = "Mehrere Sprachen",Iso2 = "mul"},
            new () {English = "Munda languages",French = "mounda, langues",German = "Mundasprachen (Andere)",Iso2 = "mun"},
            new () {English = "Creek",French = "muskogee",German = "Muskogisch",Iso2 = "mus"},
            new () {English = "Mirandese",French = "mirandais",German = "Mirandesisch",Iso2 = "mwl"},
            new () {English = "Marwari",French = "marvari",German = "Marwari",Iso2 = "mwr"},
            new () {English = "Burmese",French = "birman",German = "Birmanisch",Iso2 = "mya",Iso1 = "my"},
            new () {English = "Mayan languages",French = "maya, langues",German = "Maya-Sprachen",Iso2 = "myn"},
            new () {English = "Erzya",French = "erza",German = "Erza-Mordwinisch",Iso2 = "myv"},
            new () {English = "Nahuatl languages",French = "nahuatl, langues",German = "Nahuatl",Iso2 = "nah"},
            new () {English = "North American Indian languages",French = "nord-am\u00E9rindiennes, langues",German = "Indianersprachen, Nordamerika (Andere)",Iso2 = "nai"},
            new () {English = "Neapolitan",French = "napolitain",German = "Neapel / Mundart",Iso2 = "nap"},
            new () {English = "Nauru",French = "nauruan",German = "Nauruanisch",Iso2 = "nau",Iso1 = "na"},
            new () {English = "Navajo",French = "navaho",German = "Navajo-Sprache",Iso2 = "nav",Iso1 = "nv"},
            new () {English = "Ndebele, South",French = "nd\u00E9b\u00E9l\u00E9 du Sud",German = "Ndebele-Sprache (Transvaal)",Iso2 = "nbl",Iso1 = "nr"},
            new () {English = "Ndebele, North",French = "nd\u00E9b\u00E9l\u00E9 du Nord",German = "Ndebele-Sprache (Simbabwe)",Iso2 = "nde",Iso1 = "nd"},
            new () {English = "Ndonga",French = "ndonga",German = "Ndonga",Iso2 = "ndo",Iso1 = "ng"},
            new () {English = "Nepali",French = "n\u00E9palais",German = "Nepali",Iso2 = "nep",Iso1 = "ne"},
            new () {English = "Nepal Bhasa",French = "nepal bhasa",German = "Newari",Iso2 = "new"},
            new () {English = "Nias",French = "nias",German = "Nias-Sprache",Iso2 = "nia"},
            new () {English = "Niger-Kordofanian languages",French = "nig\u00E9ro-kordofaniennes, langues",German = "Nigerkordofanische Sprachen (Andere)",Iso2 = "nic"},
            new () {English = "Niuean",French = "niu\u00E9",German = "Niue-Sprache",Iso2 = "niu"},
            new () {English = "Dutch",French = "n\u00E9erlandais",German = "Niederl\u00E4ndisch",Iso2 = "nld",Iso1 = "nl"},
            new () {English = "Norwegian Nynorsk",French = "norv\u00E9gien nynorsk",German = "Nynorsk",Iso2 = "nno",Iso1 = "nn"},
            new () {English = "Bokm\u00E5l, Norwegian",French = "norv\u00E9gien bokm\u00E5l",German = "Bokm\u00E5l",Iso2 = "nob",Iso1 = "nb"},
            new () {English = "Nogai",French = "noga\u00EF",German = "Nogaisch",Iso2 = "nog"},
            new () {English = "Norwegian",French = "norv\u00E9gien",German = "Norwegisch",Iso2 = "nor",Iso1 = "no"},
            new () {English = "N\u0027Ko",French = "n\u0027ko",German = "N\u0027Ko",Iso2 = "nqo"},
            new () {English = "Pedi",French = "pedi",German = "Pedi-Sprache",Iso2 = "nso"},
            new () {English = "Nubian languages",French = "nubiennes, langues",German = "Nubische Sprachen",Iso2 = "nub"},
            new () {English = "Classical Newari",French = "newari classique",German = "Alt-Newari",Iso2 = "nwc"},
            new () {English = "Chichewa",French = "chichewa",German = "Nyanja-Sprache",Iso2 = "nya",Iso1 = "ny"},
            new () {English = "Nyamwezi",French = "nyamwezi",German = "Nyamwezi-Sprache",Iso2 = "nym"},
            new () {English = "Nyankole",French = "nyankol\u00E9",German = "Nkole-Sprache",Iso2 = "nyn"},
            new () {English = "Nyoro",French = "nyoro",German = "Nyoro-Sprache",Iso2 = "nyo"},
            new () {English = "Nzima",French = "nzema",German = "Nzima-Sprache",Iso2 = "nzi"},
            new () {English = "Occitan (post 1500)",French = "occitan (apr\u00E8s 1500)",German = "Okzitanisch",Iso2 = "oci",Iso1 = "oc"},
            new () {English = "Ojibwa",French = "ojibwa",German = "Ojibwa-Sprache",Iso2 = "oji",Iso1 = "oj"},
            new () {English = "Oriya",French = "oriya",German = "Oriya-Sprache",Iso2 = "ori",Iso1 = "or"},
            new () {English = "Oromo",French = "galla",German = "Galla-Sprache",Iso2 = "orm",Iso1 = "om"},
            new () {English = "Osage",French = "osage",German = "Osage-Sprache",Iso2 = "osa"},
            new () {English = "Ossetian",French = "oss\u00E8te",German = "Ossetisch",Iso2 = "oss",Iso1 = "os"},
            new () {English = "Turkish, Ottoman (1500-1928)",French = "turc ottoman (1500-1928)",German = "Osmanisch",Iso2 = "ota"},
            new () {English = "Otomian languages",French = "otomi, langues",German = "Otomangue-Sprachen",Iso2 = "oto"},
            new () {English = "Papuan languages",French = "papoues, langues",German = "Papuasprachen (Andere)",Iso2 = "paa"},
            new () {English = "Pangasinan",French = "pangasinan",German = "Pangasinan-Sprache",Iso2 = "pag"},
            new () {English = "Pahlavi",French = "pahlavi",German = "Mittelpersisch",Iso2 = "pal"},
            new () {English = "Pampanga",French = "pampangan",German = "Pampanggan-Sprache",Iso2 = "pam"},
            new () {English = "Panjabi",French = "pendjabi",German = "Pandschabi-Sprache",Iso2 = "pan",Iso1 = "pa"},
            new () {English = "Papiamento",French = "papiamento",German = "Papiamento",Iso2 = "pap"},
            new () {English = "Palauan",French = "palau",German = "Palau-Sprache",Iso2 = "pau"},
            new () {English = "Persian",French = "persan",German = "Persisch",Iso2 = "fas",Iso1 = "fa"},
            new () {English = "Philippine languages",French = "philippines, langues",German = "Philippinisch-Austronesisch (Andere)",Iso2 = "phi"},
            new () {English = "Phoenician",French = "ph\u00E9nicien",German = "Ph\u00F6nikisch",Iso2 = "phn"},
            new () {English = "Pali",French = "pali",German = "Pali",Iso2 = "pli",Iso1 = "pi"},
            new () {English = "Polish",French = "polonais",German = "Polnisch",Iso2 = "pol",Iso1 = "pl"},
            new () {English = "Pohnpeian",French = "pohnpei",German = "Ponapeanisch",Iso2 = "pon"},
            new () {English = "Portuguese",French = "portugais",German = "Portugiesisch",Iso2 = "por",Iso1 = "pt", NativeName = "Português"},
            new () {English = "Portuguese (Brazil)", French = "Portugais (Brésil)", German = "Portugiesisch (Brasilien)", Iso2 = "por-BR", Iso1 = "pt-BR"},
            new () {English = "Prakrit languages",French = "pr\u00E2krit, langues",German = "Prakrit",Iso2 = "pra"},
            new () {English = "Pushto",French = "pachto",German = "Paschtu",Iso2 = "pus",Iso1 = "ps"},
            new () {English = "Quechua",French = "quechua",German = "Quechua-Sprache",Iso2 = "que",Iso1 = "qu"},
            new () {English = "Rajasthani",French = "rajasthani",German = "Rajasthani",Iso2 = "raj"},
            new () {English = "Rapanui",French = "rapanui",German = "Osterinsel-Sprache",Iso2 = "rap"},
            new () {English = "Rarotongan",French = "rarotonga",German = "Rarotonganisch",Iso2 = "rar"},
            new () {English = "Romance languages",French = "romanes, langues",German = "Romanische Sprachen (Andere)",Iso2 = "roa"},
            new () {English = "Romansh",French = "romanche",German = "R\u00E4toromanisch",Iso2 = "roh",Iso1 = "rm"},
            new () {English = "Romany",French = "tsigane",German = "Romani (Sprache)",Iso2 = "rom"},
            new () {English = "Romanian",French = "roumain",German = "Rum\u00E4nisch",Iso2 = "ron",Iso1 = "ro"},
            new () {English = "Romanian",French = "roumain",German = "Rum\u00E4nisch",Iso2 = "ron",Iso1 = "ro"},
            new () {English = "Rundi",French = "rundi",German = "Rundi-Sprache",Iso2 = "run",Iso1 = "rn"},
            new () {English = "Aromanian",French = "aroumain",German = "Aromunisch",Iso2 = "rup"},
            new () {English = "Russian",French = "russe",German = "Russisch",Iso2 = "rus",Iso1 = "ru", NativeName = "Русский"},
            new () {English = "Sandawe",French = "sandawe",German = "Sandawe-Sprache",Iso2 = "sad"},
            new () {English = "Sango",French = "sango",German = "Sango-Sprache",Iso2 = "sag",Iso1 = "sg"},
            new () {English = "Yakut",French = "iakoute",German = "Jakutisch",Iso2 = "sah"},
            new () {English = "South American Indian languages",French = "sud-am\u00E9rindiennes, langues",German = "Indianersprachen, S\u00FCdamerika (Andere)",Iso2 = "sai"},
            new () {English = "Salishan languages",French = "salishennes, langues",German = "Salish-Sprache",Iso2 = "sal"},
            new () {English = "Samaritan Aramaic",French = "samaritain",German = "Samaritanisch",Iso2 = "sam"},
            new () {English = "Sanskrit",French = "sanskrit",German = "Sanskrit",Iso2 = "san",Iso1 = "sa"},
            new () {English = "Sasak",French = "sasak",German = "Sasak",Iso2 = "sas"},
            new () {English = "Santali",French = "santal",German = "Santali",Iso2 = "sat"},
            new () {English = "Sicilian",French = "sicilien",German = "Sizilianisch",Iso2 = "scn"},
            new () {English = "Scots",French = "\u00E9cossais",German = "Schottisch",Iso2 = "sco"},
            new () {English = "Selkup",French = "selkoupe",German = "Selkupisch",Iso2 = "sel"},
            new () {English = "Semitic languages",French = "s\u00E9mitiques, langues",German = "Semitische Sprachen (Andere)",Iso2 = "sem"},
            new () {English = "Sign Languages",French = "langues des signes",German = "Zeichensprachen",Iso2 = "sgn"},
            new () {English = "Shan",French = "chan",German = "Schan-Sprache",Iso2 = "shn"},
            new () {English = "Sidamo",French = "sidamo",German = "Sidamo-Sprache",Iso2 = "sid"},
            new () {English = "Sinhala",French = "singhalais",German = "Singhalesisch",Iso2 = "sin",Iso1 = "si"},
            new () {English = "Siouan languages",French = "sioux, langues",German = "Sioux-Sprachen (Andere)",Iso2 = "sio"},
            new () {English = "Sino-Tibetan languages",French = "sino-tib\u00E9taines, langues",German = "Sinotibetische Sprachen (Andere)",Iso2 = "sit"},
            new () {English = "Slavic languages",French = "slaves, langues",German = "Slawische Sprachen (Andere)",Iso2 = "sla"},
            new () {English = "Slovak",French = "slovaque",German = "Slowakisch",Iso2 = "slk",Iso1 = "sk"},
            new () {English = "Slovak",French = "slovaque",German = "Slowakisch",Iso2 = "slk",Iso1 = "sk"},
            new () {English = "Slovenian",French = "slov\u00E8ne",German = "Slowenisch",Iso2 = "slv",Iso1 = "sl"},
            new () {English = "Southern Sami",French = "sami du Sud",German = "S\u00FCdsaamisch",Iso2 = "sma"},
            new () {English = "Northern Sami",French = "sami du Nord",German = "Nordsaamisch",Iso2 = "sme",Iso1 = "se"},
            new () {English = "Sami languages",French = "sames, langues",German = "Saamisch",Iso2 = "smi"},
            new () {English = "Lule Sami",French = "sami de Lule",German = "Lulesaamisch",Iso2 = "smj"},
            new () {English = "Inari Sami",French = "sami d\u0027Inari",German = "Inarisaamisch",Iso2 = "smn"},
            new () {English = "Samoan",French = "samoan",German = "Samoanisch",Iso2 = "smo",Iso1 = "sm"},
            new () {English = "Skolt Sami",French = "sami skolt",German = "Skoltsaamisch",Iso2 = "sms"},
            new () {English = "Shona",French = "shona",German = "Schona-Sprache",Iso2 = "sna",Iso1 = "sn"},
            new () {English = "Sindhi",French = "sindhi",German = "Sindhi-Sprache",Iso2 = "snd",Iso1 = "sd"},
            new () {English = "Soninke",French = "sonink\u00E9",German = "Soninke-Sprache",Iso2 = "snk"},
            new () {English = "Sogdian",French = "sogdien",German = "Sogdisch",Iso2 = "sog"},
            new () {English = "Somali",French = "somali",German = "Somali",Iso2 = "som",Iso1 = "so"},
            new () {English = "Songhai languages",French = "songhai, langues",German = "Songhai-Sprache",Iso2 = "son"},
            new () {English = "Sotho, Southern",French = "sotho du Sud",German = "S\u00FCd-Sotho-Sprache",Iso2 = "sot",Iso1 = "st"},
            new () {English = "Spanish",French = "espagnol",German = "Spanisch",Iso2 = "spa",Iso1 = "es", NativeName = "Español"},
            new () {English = "Spanish (Latin America)", French = "Espagnol (Amérique latine) ", German = "Spanisch (Lateinamerika)", Iso2 = "spa-419", Iso1 = "es-419"},
            new () {English = "Albanian",French = "albanais",German = "Albanisch",Iso2 = "sqi",Iso1 = "sq"},
            new () {English = "Sardinian",French = "sarde",German = "Sardisch",Iso2 = "srd",Iso1 = "sc"},
            new () {English = "Sranan Tongo",French = "sranan tongo",German = "Sranantongo",Iso2 = "srn"},
            new () {English = "Serbian",French = "serbe",German = "Serbisch",Iso2 = "srp",Iso1 = "sr"},
            new () {English = "Serer",French = "s\u00E9r\u00E8re",German = "Serer-Sprache",Iso2 = "srr"},
            new () {English = "Nilo-Saharan languages",French = "nilo-sahariennes, langues",German = "Nilosaharanische Sprachen (Andere)",Iso2 = "ssa"},
            new () {English = "Swati",French = "swati",German = "Swasi-Sprache",Iso2 = "ssw",Iso1 = "ss"},
            new () {English = "Sukuma",French = "sukuma",German = "Sukuma-Sprache",Iso2 = "suk"},
            new () {English = "Sundanese",French = "soundanais",German = "Sundanesisch",Iso2 = "sun",Iso1 = "su"},
            new () {English = "Susu",French = "soussou",German = "Susu",Iso2 = "sus"},
            new () {English = "Sumerian",French = "sum\u00E9rien",German = "Sumerisch",Iso2 = "sux"},
            new () {English = "Swahili",French = "swahili",German = "Swahili",Iso2 = "swa",Iso1 = "sw"},
            new () {English = "Swedish",French = "su\u00E9dois",German = "Schwedisch",Iso2 = "swe",Iso1 = "sv", NativeName = "Svenska"},
            new () {English = "Classical Syriac",French = "syriaque classique",German = "Syrisch",Iso2 = "syc"},
            new () {English = "Syriac",French = "syriaque",German = "Neuostaram\u00E4isch",Iso2 = "syr"},
            new () {English = "Tahitian",French = "tahitien",German = "Tahitisch",Iso2 = "tah",Iso1 = "ty"},
            new () {English = "Tai languages",French = "tai, langues",German = "Thaisprachen (Andere)",Iso2 = "tai"},
            new () {English = "Tamil",French = "tamoul",German = "Tamil",Iso2 = "tam",Iso1 = "ta"},
            new () {English = "Tatar",French = "tatar",German = "Tatarisch",Iso2 = "tat",Iso1 = "tt"},
            new () {English = "Telugu",French = "t\u00E9lougou",German = "Telugu-Sprache",Iso2 = "tel",Iso1 = "te"},
            new () {English = "Timne",French = "temne",German = "Temne-Sprache",Iso2 = "tem"},
            new () {English = "Tereno",French = "tereno",German = "Tereno-Sprache",Iso2 = "ter"},
            new () {English = "Tetum",French = "tetum",German = "Tetum-Sprache",Iso2 = "tet"},
            new () {English = "Tajik",French = "tadjik",German = "Tadschikisch",Iso2 = "tgk",Iso1 = "tg"},
            new () {English = "Tagalog",French = "tagalog",German = "Tagalog",Iso2 = "tgl",Iso1 = "tl"},
            new () {English = "Thai",French = "tha\u00EF",German = "Thail\u00E4ndisch",Iso2 = "tha",Iso1 = "th"},
            new () {English = "Tibetan",French = "tib\u00E9tain",German = "Tibetisch",Iso2 = "bod",Iso1 = "bo"},
            new () {English = "Tigre",French = "tigr\u00E9",German = "Tigre-Sprache",Iso2 = "tig"},
            new () {English = "Tigrinya",French = "tigrigna",German = "Tigrinja-Sprache",Iso2 = "tir",Iso1 = "ti"},
            new () {English = "Tiv",French = "tiv",German = "Tiv-Sprache",Iso2 = "tiv"},
            new () {English = "Tokelau",French = "tokelau",German = "Tokelauanisch",Iso2 = "tkl"},
            new () {English = "Klingon",French = "klingon",German = "Klingonisch",Iso2 = "tlh"},
            new () {English = "Tlingit",French = "tlingit",German = "Tlingit-Sprache",Iso2 = "tli"},
            new () {English = "Tamashek",French = "tamacheq",German = "Tama\u009Aeq",Iso2 = "tmh"},
            new () {English = "Tonga (Nyasa)",French = "tonga (Nyasa)",German = "Tonga (Bantusprache, Sambia)",Iso2 = "tog"},
            new () {English = "Tonga (Tonga Islands)",French = "tongan (\u00CEles Tonga)",German = "Tongaisch",Iso2 = "ton",Iso1 = "to"},
            new () {English = "Tok Pisin",French = "tok pisin",German = "Neumelanesisch",Iso2 = "tpi"},
            new () {English = "Tsimshian",French = "tsimshian",German = "Tsimshian-Sprache",Iso2 = "tsi"},
            new () {English = "Tswana",French = "tswana",German = "Tswana-Sprache",Iso2 = "tsn",Iso1 = "tn"},
            new () {English = "Tsonga",French = "tsonga",German = "Tsonga-Sprache",Iso2 = "tso",Iso1 = "ts"},
            new () {English = "Turkmen",French = "turkm\u00E8ne",German = "Turkmenisch",Iso2 = "tuk",Iso1 = "tk"},
            new () {English = "Tumbuka",French = "tumbuka",German = "Tumbuka-Sprache",Iso2 = "tum"},
            new () {English = "Tupi languages",French = "tupi, langues",German = "Tupi-Sprache",Iso2 = "tup"},
            new () {English = "Turkish",French = "turc",German = "T\u00FCrkisch",Iso2 = "tur",Iso1 = "tr"},
            new () {English = "Altaic languages",French = "alta\u00EFques, langues",German = "Altaische Sprachen (Andere)",Iso2 = "tut"},
            new () {English = "Tuvalu",French = "tuvalu",German = "Elliceanisch",Iso2 = "tvl"},
            new () {English = "Twi",French = "twi",German = "Twi-Sprache",Iso2 = "twi",Iso1 = "tw"},
            new () {English = "Tuvinian",French = "touva",German = "Tuwinisch",Iso2 = "tyv"},
            new () {English = "Udmurt",French = "oudmourte",German = "Udmurtisch",Iso2 = "udm"},
            new () {English = "Ugaritic",French = "ougaritique",German = "Ugaritisch",Iso2 = "uga"},
            new () {English = "Uighur",French = "ou\u00EFgour",German = "Uigurisch",Iso2 = "uig",Iso1 = "ug"},
            new () {English = "Ukrainian",French = "ukrainien",German = "Ukrainisch",Iso2 = "ukr",Iso1 = "uk"},
            new () {English = "Umbundu",French = "umbundu",German = "Mbundu-Sprache",Iso2 = "umb"},
            new () {English = "Undetermined",French = "ind\u00E9termin\u00E9e",German = "Nicht zu entscheiden",Iso2 = "und"},
            new () {English = "Urdu",French = "ourdou",German = "Urdu",Iso2 = "urd",Iso1 = "ur"},
            new () {English = "Uzbek",French = "ouszbek",German = "Usbekisch",Iso2 = "uzb",Iso1 = "uz"},
            new () {English = "Vai",French = "va\u00EF",German = "Vai-Sprache",Iso2 = "vai"},
            new () {English = "Venda",French = "venda",German = "Venda-Sprache",Iso2 = "ven",Iso1 = "ve"},
            new () {English = "Vietnamese",French = "vietnamien",German = "Vietnamesisch",Iso2 = "vie",Iso1 = "vi"},
            new () {English = "Volap\u00FCk",French = "volap\u00FCk",German = "Volap\u00FCk",Iso2 = "vol",Iso1 = "vo"},
            new () {English = "Votic",French = "vote",German = "Wotisch",Iso2 = "vot"},
            new () {English = "Wakashan languages",French = "wakashanes, langues",German = "Wakash-Sprachen",Iso2 = "wak"},
            new () {English = "Wolaitta",French = "wolaitta",German = "Walamo-Sprache",Iso2 = "wal"},
            new () {English = "Waray",French = "waray",German = "Waray",Iso2 = "war"},
            new () {English = "Washo",French = "washo",German = "Washo-Sprache",Iso2 = "was"},
            new () {English = "Welsh",French = "gallois",German = "Kymrisch",Iso2 = "cym",Iso1 = "cy"},
            new () {English = "Sorbian languages",French = "sorabes, langues",German = "Sorbisch (Andere)",Iso2 = "wen"},
            new () {English = "Walloon",French = "wallon",German = "Wallonisch",Iso2 = "wln",Iso1 = "wa"},
            new () {English = "Wolof",French = "wolof",German = "Wolof-Sprache",Iso2 = "wol",Iso1 = "wo"},
            new () {English = "Kalmyk",French = "kalmouk",German = "Kalm\u00FCckisch",Iso2 = "xal"},
            new () {English = "Xhosa",French = "xhosa",German = "Xhosa-Sprache",Iso2 = "xho",Iso1 = "xh"},
            new () {English = "Yao",French = "yao",German = "Yao-Sprache (Bantusprache)",Iso2 = "yao"},
            new () {English = "Yapese",French = "yapois",German = "Yapesisch",Iso2 = "yap"},
            new () {English = "Yiddish",French = "yiddish",German = "Jiddisch",Iso2 = "yid",Iso1 = "yi"},
            new () {English = "Yoruba",French = "yoruba",German = "Yoruba-Sprache",Iso2 = "yor",Iso1 = "yo"},
            new () {English = "Yupik languages",French = "yupik, langues",German = "Ypik-Sprachen",Iso2 = "ypk"},
            new () {English = "Zapotec",French = "zapot\u00E8que",German = "Zapotekisch",Iso2 = "zap"},
            new () {English = "Blissymbols",French = "symboles Bliss",German = "Bliss-Symbol",Iso2 = "zbl"},
            new () {English = "Zenaga",French = "zenaga",German = "Zenaga",Iso2 = "zen"},
            new () {English = "Standard Moroccan Tamazight",French = "amazighe standard marocain",German = "Standard-marokkanischen Tamazight",Iso2 = "zgh"},
            new () {English = "Zhuang",French = "zhuang",German = "Zhuang",Iso2 = "zha",Iso1 = "za"},
            new () {English = "Chinese",French = "chinois",German = "Chinesisch",Iso2 = "zho",Iso1 = "zh"},
            new () {English = "Zande languages",French = "zand\u00E9, langues",German = "Zande-Sprachen",Iso2 = "znd"},
            new () {English = "Zulu",French = "zoulou",German = "Zulu-Sprache",Iso2 = "zul",Iso1 = "zu"},
            new () {English = "Zuni",French = "zuni",German = "Zu\u00F1i-Sprache",Iso2 = "zun"},
            new () {English = "No linguistic content",French = "pas de contenu linguistique",German = "Kein linguistischer Inhalt",Iso2 = "zxx"},
            new () {English = "Zaza",French = "zaza",German = "Zazaki",Iso2 = "zza"}
        };
    }

    static LanguageDefintion FindLanguage(string language)
    {
        if(string.IsNullOrWhiteSpace(language))
            return new LanguageDefintion() { English = language, French = language, German = language, Iso1 = language, Iso2 = language};

        var ll = language.ToLowerInvariant();

        var lang = Languages.FirstOrDefault(x =>
        {
            if (x.Iso1 == ll)
                return true;
            if (x.Iso2 == ll)
                return true;
            if(x.English?.ToLowerInvariant() == ll)
                return true;
            if (x.French?.ToLowerInvariant() == ll)
                return true;
            if (x.German?.ToLowerInvariant() == ll)
                return true;
            if (x.Aliases?.Contains(ll) == true)
                return true;
            return false;
        });
        return lang;
    }
    
    /// <summary>
    /// Gets the english name for a language
    /// </summary>
    /// <param name="language">language the language to lookup</param>
    /// <returns>the english name, if not known will return the original name</returns>
    public static string GetEnglishFor(string language)
    {     
        var lang = FindLanguage(language);
        return lang?.English?.EmptyAsNull() ?? language;
    }

    /// <summary>
    /// Gets the native name for a language
    /// </summary>
    /// <param name="language">the language to get the native name for</param>
    /// <returns>the native name of a language</returns>
    public static string GetNativeName(string language)
    {
        var lang = FindLanguage(language);
        return lang?.NativeName?.EmptyAsNull() ?? lang?.English?.EmptyAsNull() ?? language;
    }
    
    /// <summary>
    /// Gets the ISO-639-1 code for a language
    /// </summary>
    /// <param name="language">language the language to lookup</param>
    /// <returns>the ISO-639-1 code, if not known will return the original name</returns>
    public static string GetIso1Code(string language) 
    {
        var lang = FindLanguage(language);
        return lang?.Iso1?.EmptyAsNull() ?? language;
    }
    
    /// <summary>
    /// Gets the ISO-639-2 code for a language
    /// </summary>
    /// <param name="language">language the language to lookup</param>
    /// <returns>the ISO-639-2 code, if not known will return the original name</returns>
    public static string GetIso2Code(string language) 
    {
        var lang = FindLanguage(language);
        return lang?.Iso2?.EmptyAsNull() ?? language;
    }
    
    /// <summary>
    /// Tests if two languages are the same
    /// </summary>
    /// <param name="langOne">the first language</param>
    /// <param name="langTwo">the second language</param>
    /// <returns>true if matches, otherwise false</returns>
    public static bool AreSame(string langOne, string langTwo)
    {
        if (string.IsNullOrWhiteSpace(langTwo))
            return false;
        if (string.IsNullOrWhiteSpace(langOne))
            return false;
        if (langTwo.ToLowerInvariant().Contains(langOne.ToLowerInvariant()))
            return true;
        try
        {
            if (GetIso2Code(langOne) == GetIso2Code(langTwo))
                return true;
        }
        catch (Exception)
        {
        }

        try
        {
            if (GetIso1Code(langOne) == GetIso1Code(langTwo))
                return true;
        }
        catch (Exception)
        {
        }

        try
        {
            var rgx = new Regex(langTwo, RegexOptions.IgnoreCase);
            return rgx.IsMatch(langOne);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Tests if a language
    /// </summary>
    /// <param name="comparison">the string comparison eg /en|fr/</param>
    /// <param name="value">the value of the language, eg en, or english, or eng etc</param>
    /// <returns>true if matches, otherwise false</returns>
    public static bool Matches(NodeParameters args, string comparison, string value)
    {
        bool inverse = comparison.StartsWith('!');
        if (inverse)
            comparison = comparison[1..];
        bool result = RunComparison();
        return inverse ? !result : result;
        
        bool RunComparison ()
        {
            if (Regex.IsMatch(comparison,
                    @"^/([a-z]{2}(-[a-z]{2})?|[a-zA-Z\-]+)(\|([a-z]{2}(-[a-z]{2})?|[a-zA-Z\-]+))*?/$"))
            {
                // Remove the leading and trailing slashes
                string innerContent = comparison.Trim('/');

                // Split the inner content by '|'
                var languages = innerContent.Split('|');

                // Loop through each language
                foreach (var language in languages)
                {
                    // Process each individual language
                    if (Matches(args, language, value))
                        return true;
                }

                return false;
            }

            comparison = args.ReplaceVariables(comparison.Replace("{orig}", "{OriginalLanguage}"), stripMissing: false);
            if (args.Variables.TryGetValue("OriginalLanguage", out var oOrigLanguage) &&
                oOrigLanguage is string origLanguage &&
                string.IsNullOrWhiteSpace(origLanguage) == false)
            {
                comparison = comparison.Replace("OriginalLanguage", origLanguage,
                    StringComparison.InvariantCultureIgnoreCase);
                comparison = comparison.Replace("original", origLanguage);
                comparison = comparison.Replace("orig", origLanguage);
            }

            var strMatch = comparison.Equals(value, StringComparison.InvariantCultureIgnoreCase);
            if (strMatch)
                return true;
            // Check if comparison starts with a letter and contains only valid characters afterwards
            if (Regex.IsMatch(comparison, @"^[a-zA-Z][a-zA-Z\-\(\)\{\}]*$"))
            {
                comparison = "=" + comparison;
            }

            string iso1 = GetIso1Code(value);
            string iso2 = GetIso2Code(value);
            string english = GetEnglishFor(value);
            var iso1Matches = ValueMatch(comparison, iso1);
            var iso2Matches = ValueMatch(comparison, iso2);
            var engMatches = ValueMatch(comparison, english);

            bool anyMatches = iso1Matches || iso2Matches || engMatches;
            if (anyMatches == false)
            {
                args.Logger.ILog("Language does not match: " + value);
                return false;
            }

            if (iso1Matches)
                args.Logger?.ILog($"Language ISO-1 match found: '{iso1}' vs '{comparison}'");
            if (iso2Matches)
                args.Logger?.ILog($"Language ISO-2 match found: '{iso2}' vs '{comparison}'");
            if (engMatches)
                args.Logger?.ILog($"Language English match found: '{english}' vs '{comparison}'");
            return true;
        }

        bool ValueMatch(string pattern, string value)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return false;
            if (string.IsNullOrEmpty(value))
                return false;
            return args.StringHelper.Matches(pattern, value);
        }
    }
    
    /// <summary>
    /// Represents a language definition with various properties such as English, French, German names, 
    /// ISO-639-1 and ISO-639-2 codes, native name, and aliases.
    /// </summary>
    public class LanguageDefintion
    {
        /// <summary>
        /// Gets or sets the English name of the language.
        /// </summary>
        public string English { get; set; }

        /// <summary>
        /// Gets or sets the French name of the language.
        /// </summary>
        public string French { get; set; }

        /// <summary>
        /// Gets or sets the German name of the language.
        /// </summary>
        public string German { get; set; }

        /// <summary>
        /// Gets or sets the native name of the language.
        /// </summary>
        public string NativeName { get; set; }

        /// <summary>
        /// Gets or sets the ISO-639-1 code of the language.
        /// </summary>
        public string Iso1 { get; set; }

        /// <summary>
        /// Gets or sets the ISO-639-2 code of the language.
        /// </summary>
        public string Iso2 { get; set; }

        /// <summary>
        /// Gets or sets the aliases of the language.
        /// </summary>
        public string[] Aliases { get; set; }
    }
    
}