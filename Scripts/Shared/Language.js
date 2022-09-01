/**
 * Helper class for Languages
 * @name Language
 * @revision 1
 * @minimumVersion 1.0.0.0
 */
 export class Language
 {    
    /**
     * Gets the english name for a language
     * @param {string} language the language to lookup
     * @returns the english name, if not known will return the original name
     */
    getEnglishFor(language){        
        let lang = this.Languages.filter(x => x.iso === language.toLowerCase() || x.english.toLowerCase() === language.toLowerCase());
        if(lang?.length)
            return lang[0].english;
        // try some common ones
        if(language.toLowerCase() === 'eng')
            return 'English';
        if(language.toLowerCase() === 'deu')
            return 'German';
        if(language.toLowerCase() === 'fre')
            return 'French';
        return language;
    }
    
    /**
     * Gets the ISO-639-1 code for a language
     * @param {string} language the language to lookup
     * @returns the ISO-639-1 code, if not known will return the original name
     */
    getIsoCode(language) {
        let lang = this.Languages.filter(x => x.iso === language.toLowerCase() || x.english.toLowerCase() === language.toLowerCase());
        if(lang.length)
            return lang[0].iso;
        // try some common ones
        if(language.toLowerCase().startsWith('eng'))
            return 'en';
        return language;
    }

    constructor()
    {
        this.Languages = [
            { english: 'Abkhazian', iso: 'ab' },
            { english: 'Afar', iso: 'aa' },
            { english: 'Afrikaans', iso: 'af' },
            { english: 'Akan', iso: 'ak' },
            { english: 'Albanian', iso: 'sq' },
            { english: 'Amharic', iso: 'am' },
            { english: 'Arabic', iso: 'ar' },
            { english: 'Aragonese', iso: 'an' },
            { english: 'Armenian', iso: 'hy' },
            { english: 'Assamese', iso: 'as' },
            { english: 'Avaric', iso: 'av' },
            { english: 'Avestan', iso: 'ae' },
            { english: 'Aymara', iso: 'ay' },
            { english: 'Azerbaijani', iso: 'az' },
            { english: 'Bambara', iso: 'bm' },
            { english: 'Bashkir', iso: 'ba' },
            { english: 'Basque', iso: 'eu' },
            { english: 'Belarusian', iso: 'be' },
            { english: 'Bengali (Bangla)', iso: 'bn' },
            { english: 'Bihari', iso: 'bh' },
            { english: 'Bislama', iso: 'bi' },
            { english: 'Bosnian', iso: 'bs' },
            { english: 'Breton', iso: 'br' },
            { english: 'Bulgarian', iso: 'bg' },
            { english: 'Burmese', iso: 'my' },
            { english: 'Catalan', iso: 'ca' },
            { english: 'Chamorro', iso: 'ch' },
            { english: 'Chechen', iso: 'ce' },
            { english: 'Chichewa Chewa, Nyanja', iso: 'ny' },
            { english: 'Chinese', iso: 'zh' },
            { english: 'Chinese (Simplified)', iso: 'zh-Hans' },
            { english: 'Chinese (Traditional)', iso: 'zh-Hant' },
            { english: 'Chuvash', iso: 'cv' },
            { english: 'Cornish', iso: 'kw' },
            { english: 'Corsican', iso: 'co' },
            { english: 'Cree', iso: 'cr' },
            { english: 'Croatian', iso: 'hr' },
            { english: 'Czech', iso: 'cs' },
            { english: 'Danish', iso: 'da' },
            { english: 'Divehi Dhivehi, Maldivian', iso: 'dv' },
            { english: 'Dutch', iso: 'nl' },
            { english: 'Dzongkha', iso: 'dz' },
            { english: 'English', iso: 'en' },
            { english: 'Esperanto', iso: 'eo' },
            { english: 'Estonian', iso: 'et' },
            { english: 'Ewe', iso: 'ee' },
            { english: 'Faroese', iso: 'fo' },
            { english: 'Fijian', iso: 'fj' },
            { english: 'Finnish', iso: 'fi' },
            { english: 'French', iso: 'fr' },
            { english: 'Fula Fulah, Pulaar, Pular', iso: 'ff' },
            { english: 'Galician', iso: 'gl' },
            { english: 'Gaelic (Scottish)', iso: 'gd' },
            { english: 'Gaelic (Manx)', iso: 'gv' },
            { english: 'Georgian', iso: 'ka' },
            { english: 'German', iso: 'de' },
            { english: 'Greek', iso: 'el' },
            { english: 'Greenlandic', iso: 'kl' },
            { english: 'Guarani', iso: 'gn' },
            { english: 'Gujarati', iso: 'gu' },
            { english: 'Haitian Creole', iso: 'ht' },
            { english: 'Hausa', iso: 'ha' },
            { english: 'Hebrew', iso: 'he' },
            { english: 'Herero', iso: 'hz' },
            { english: 'Hindi', iso: 'hi' },
            { english: 'Hiri Motu', iso: 'ho' },
            { english: 'Hungarian', iso: 'hu' },
            { english: 'Icelandic', iso: 'is' },
            { english: 'Ido', iso: 'io' },
            { english: 'Igbo', iso: 'ig' },
            { english: 'Indonesian', iso: 'id' },
            { english: 'Interlingua', iso: 'ia' },
            { english: 'Interlingue', iso: 'ie' },
            { english: 'Inuktitut', iso: 'iu' },
            { english: 'Inupiak', iso: 'ik' },
            { english: 'Irish', iso: 'ga' },
            { english: 'Italian', iso: 'it' },
            { english: 'Japanese', iso: 'ja' },
            { english: 'Javanese', iso: 'jv' },
            { english: 'Kalaallisut Greenlandic', iso: 'kl' },
            { english: 'Kannada', iso: 'kn' },
            { english: 'Kanuri', iso: 'kr' },
            { english: 'Kashmiri', iso: 'ks' },
            { english: 'Kazakh', iso: 'kk' },
            { english: 'Khmer', iso: 'km' },
            { english: 'Kikuyu', iso: 'ki' },
            { english: 'Kinyarwanda (Rwanda)', iso: 'rw' },
            { english: 'Kirundi', iso: 'rn' },
            { english: 'Kyrgyz', iso: 'ky' },
            { english: 'Komi', iso: 'kv' },
            { english: 'Kongo', iso: 'kg' },
            { english: 'Korean', iso: 'ko' },
            { english: 'Kurdish', iso: 'ku' },
            { english: 'Kwanyama', iso: 'kj' },
            { english: 'Lao', iso: 'lo' },
            { english: 'Latin', iso: 'la' },
            { english: 'Latvian (Lettish)', iso: 'lv' },
            { english: 'Limburgish (Limburger)', iso: 'li' },
            { english: 'Lingala', iso: 'ln' },
            { english: 'Lithuanian', iso: 'lt' },
            { english: 'Luga Katanga', iso: 'lu' },
            { english: 'Luganda Ganda', iso: 'lg' },
            { english: 'Luxembourgish', iso: 'lb' },
            { english: 'Manx', iso: 'gv' },
            { english: 'Macedonian', iso: 'mk' },
            { english: 'Malagasy', iso: 'mg' },
            { english: 'Malay', iso: 'ms' },
            { english: 'Malayalam', iso: 'ml' },
            { english: 'Maltese', iso: 'mt' },
            { english: 'Maori', iso: 'mi' },
            { english: 'Marathi', iso: 'mr' },
            { english: 'Marshallese', iso: 'mh' },
            { english: 'Moldavian', iso: 'mo' },
            { english: 'Mongolian', iso: 'mn' },
            { english: 'Nauru', iso: 'na' },
            { english: 'Navajo', iso: 'nv' },
            { english: 'Ndonga', iso: 'ng' },
            { english: 'Northern Ndebele', iso: 'nd' },
            { english: 'Nepali', iso: 'ne' },
            { english: 'Norwegian', iso: 'no' },
            { english: 'Norwegian bokmål', iso: 'nb' },
            { english: 'Norwegian nynorsk', iso: 'nn' },
            { english: 'Nuosu', iso: 'ii' },
            { english: 'Occitan', iso: 'oc' },
            { english: 'Ojibwe', iso: 'oj' },
            { english: 'Old Church Slavonic, Old Bulgarian', iso: 'cu' },
            { english: 'Oriya', iso: 'or' },
            { english: 'Oromo (Afaan Oromo)', iso: 'om' },
            { english: 'Ossetian', iso: 'os' },
            { english: 'Pāli', iso: 'pi' },
            { english: 'Pashto Pushto', iso: 'ps' },
            { english: 'Persian (Farsi)', iso: 'fa' },
            { english: 'Polish', iso: 'pl' },
            { english: 'Portuguese', iso: 'pt' },
            { english: 'Punjabi (Eastern)', iso: 'pa' },
            { english: 'Quechua', iso: 'qu' },
            { english: 'Romansh', iso: 'rm' },
            { english: 'Romanian', iso: 'ro' },
            { english: 'Russian', iso: 'ru' },
            { english: 'Sami', iso: 'se' },
            { english: 'Samoan', iso: 'sm' },
            { english: 'Sango', iso: 'sg' },
            { english: 'Sanskrit', iso: 'sa' },
            { english: 'Serbian', iso: 'sr' },
            { english: 'Serbo Croatian', iso: 'sh' },
            { english: 'Sesotho', iso: 'st' },
            { english: 'Setswana', iso: 'tn' },
            { english: 'Shona', iso: 'sn' },
            { english: 'Sichuan', iso: 'Yi	ii' },
            { english: 'Sindhi', iso: 'sd' },
            { english: 'Sinhalese', iso: 'si' },
            { english: 'Siswati', iso: 'ss' },
            { english: 'Slovak', iso: 'sk' },
            { english: 'Slovenian', iso: 'sl' },
            { english: 'Somali', iso: 'so' },
            { english: 'Southern Ndebele', iso: 'nr' },
            { english: 'Spanish', iso: 'es' },
            { english: 'Sundanese', iso: 'su' },
            { english: 'Swahili (Kiswahili)', iso: 'sw' },
            { english: 'Swati', iso: 'ss' },
            { english: 'Swedish', iso: 'sv' },
            { english: 'Tagalog', iso: 'tl' },
            { english: 'Tahitian', iso: 'ty' },
            { english: 'Tajik', iso: 'tg' },
            { english: 'Tamil', iso: 'ta' },
            { english: 'Tatar', iso: 'tt' },
            { english: 'Telugu', iso: 'te' },
            { english: 'Thai', iso: 'th' },
            { english: 'Tibetan', iso: 'bo' },
            { english: 'Tigrinya', iso: 'ti' },
            { english: 'Tonga', iso: 'to' },
            { english: 'Tsonga', iso: 'ts' },
            { english: 'Turkish', iso: 'tr' },
            { english: 'Turkmen', iso: 'tk' },
            { english: 'Twi', iso: 'tw' },
            { english: 'Uyghur', iso: 'ug' },
            { english: 'Ukrainian', iso: 'uk' },
            { english: 'Urdu', iso: 'ur' },
            { english: 'Uzbek', iso: 'uz' },
            { english: 'Venda', iso: 've' },
            { english: 'Vietnamese', iso: 'vi' },
            { english: 'Volapük', iso: 'vo' },
            { english: 'Wallon', iso: 'wa' },
            { english: 'Welsh', iso: 'cy' },
            { english: 'Wolof', iso: 'wo' },
            { english: 'Western Frisian', iso: 'fy' },
            { english: 'Xhosa', iso: 'xh' },
            { english: 'Yiddish', iso: 'yi, ji' },
            { english: 'Yoruba', iso: 'yo' },
            { english: 'Zhuang Chuang', iso: 'za' },
            { english: 'Zulu', iso: 'zu' }
        ];
    }
 }