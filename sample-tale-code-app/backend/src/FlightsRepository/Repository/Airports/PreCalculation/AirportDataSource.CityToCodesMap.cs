﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DreamTravel.FlightProviderData.Repository.Airports.PreCalculation
{
    public partial class AirportDataSource
    {
        public static Dictionary<string, string> GetCityToCodeMap()
        {
            var airportToCityMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(Airports2Countries);

            return airportToCityMap.ToDictionary(k => k.Value, v => v.Key);
        }

        #region Airports2Countries json

        private const string Airports2Countries = @"
{
""POM"" :  ""Port Moresby"",
""IFJ"" :  ""Isafjordur"",
""KEF"" :  ""Reykjavik (Keflavik)"",
""YUL"" :  ""Montreal"",
""YYZ"" :  ""Toronto Pearson"",
""BJA"" :  ""Soummam (Bejaja)"",
""ALG"" :  ""Algiers"",
""CZL"" :  ""Constantine"",
""TLM"" :  ""Tlemcen"",
""ORN"" :  ""Oran"",
""MIR"" :  ""Monastir"",
""TUN"" :  ""Tunis"",
""DJE"" :  ""Djerba"",
""ANR"" :  ""Antwerp-Deurne"",
""BRU"" :  ""Brussels"",
""CRL"" :  ""Brussels (Charleroi)"",
""LGG"" :  ""Liege"",
""OST"" :  ""Ostend-Bruges"",
""SXF"" :  ""Berlin (Schonefeld)"",
""DRS"" :  ""Dresden"",
""ERF"" :  ""Erfurt"",
""FRA"" :  ""Frankfurt"",
""FMO"" :  ""Muenster-Osnabrueck"",
""HAM"" :  ""Hamburg"",
""CGN"" :  ""Cologne-Bonn"",
""DUS"" :  ""Duesseldorf"",
""MUC"" :  ""Munich"",
""NUE"" :  ""Nuernberg"",
""LEJ"" :  ""Leipzig-Halle"",
""SCN"" :  ""Saarbruecken"",
""STR"" :  ""Stuttgart"",
""TXL"" :  ""Berlin (Tegel)"",
""HAJ"" :  ""Hanover"",
""BRE"" :  ""Bremen"",
""HHN"" :  ""Frankfurt (Hahn)"",
""PAD"" :  ""Paderborn"",
""DTM"" :  ""Dortmund"",
""FDH"" :  ""Friedrichshafen"",
""GWT"" :  ""Westerland-Sylt"",
""TLL"" :  ""Tallinn"",
""HEL"" :  ""Helsinki"",
""KTT"" :  ""Kittila"",
""LPP"" :  ""Lappeenranta"",
""OUL"" :  ""Oulu"",
""RVN"" :  ""Rovaniemi"",
""TMP"" :  ""Tampere"",
""TKU"" :  ""Turku"",
""BFS"" :  ""Belfast"",
""BHD"" :  ""Belfast (city)"",
""LDY"" :  ""Derry"",
""BHX"" :  ""Birmingham"",
""MAN"" :  ""Manchester"",
""NQY"" :  ""Newquay"",
""CWL"" :  ""Cardiff"",
""BRS"" :  ""Bristol"",
""LPL"" :  ""Liverpool"",
""LTN"" :  ""London (Luton)"",
""BOH"" :  ""Bournemouth"",
""SOU"" :  ""Southampton"",
""GCI"" :  ""Guernsey"",
""JER"" :  ""Jersey"",
""LGW"" :  ""London (Gatwick)"",
""LCY"" :  ""London (City)"",
""LHR"" :  ""London (Heathrow)"",
""SEN"" :  ""London (Southend)"",
""HUY"" :  ""Humberside"",
""LBA"" :  ""Leeds"",
""IOM"" :  ""Isle Of Man"",
""NCL"" :  ""Newcastle"",
""MME"" :  ""Durham-Tees"",
""EMA"" :  ""East Midlands"",
""WIC"" :  ""Wick"",
""ABZ"" :  ""Aberdeen"",
""INV"" :  ""Inverness"",
""GLA"" :  ""Glasgow"",
""EDI"" :  ""Edinburgh"",
""PIK"" :  ""Glasgow (Prestwick)"",
""NWI"" :  ""Norwich"",
""STN"" :  ""London (Stansted)"",
""EXT"" :  ""Exeter"",
""AMS"" :  ""Amsterdam"",
""MST"" :  ""Maastricht-Aachen"",
""EIN"" :  ""Eindhoven"",
""GRQ"" :  ""Groningen"",
""RTM"" :  ""Rotterdam"",
""ORK"" :  ""Cork"",
""DUB"" :  ""Dublin"",
""NOC"" :  ""Knock"",
""KIR"" :  ""Farranfore"",
""SNN"" :  ""Shannon"",
""AAR"" :  ""Aarhus"",
""BLL"" :  ""Billund"",
""CPH"" :  ""Copenhagen"",
""FAE"" :  ""Vagar"",
""AAL"" :  ""Aalborg"",
""LUX"" :  ""Luxembourg"",
""AES"" :  ""Aalesund"",
""ANX"" :  ""Andenes"",
""ALF"" :  ""Alta"",
""BNN"" :  ""Bronnoysund"",
""BOO"" :  ""Bodo"",
""BGO"" :  ""Bergen"",
""BJF"" :  ""Batsfjord"",
""KRS"" :  ""Kristiansand"",
""BDU"" :  ""Bardufoss"",
""EVE"" :  ""Harstad-Narvik (Evenes)"",
""FRO"" :  ""Floro"",
""OSL"" :  ""Oslo (Gardermoen)"",
""HAU"" :  ""Haugesund"",
""HAA"" :  ""Hasvik"",
""KSU"" :  ""Kristiansund"",
""KKN"" :  ""Kirkenes"",
""MOL"" :  ""Molde"",
""MJF"" :  ""Mosjoen"",
""LKL"" :  ""Lakselv (Banak)"",
""RRS"" :  ""Roros"",
""LYR"" :  ""Longyearbyen-Svalbard"",
""SSJ"" :  ""Sandnessjoen"",
""TOS"" :  ""Tromso"",
""TRF"" :  ""Oslo (Torp)"",
""TRD"" :  ""Trondheim"",
""SVG"" :  ""Stavanger"",
""SZY"" :  ""Olsztyn-Mazury"",
""GDN"" :  ""Gdansk"",
""KRK"" :  ""Krakow"",
""KTW"" :  ""Katowice"",
""POZ"" :  ""Poznan"",
""RZE"" :  ""Rzeszow-Jasionka"",
""SZZ"" :  ""Szczecin"",
""WAW"" :  ""Warsaw"",
""WRO"" :  ""Wroclaw"",
""GOT"" :  ""Goteborg (Landvetter)"",
""NYO"" :  ""Stockholm (Skavsta)"",
""MMX"" :  ""Malmo"",
""VXO"" :  ""Vaxjo"",
""KRN"" :  ""Kiruna"",
""UME"" :  ""Umea"",
""VST"" :  ""Stockholm (Vasteras)"",
""LLA"" :  ""Lulea"",
""ARN"" :  ""Stockholm (Arlanda)"",
""VBY"" :  ""Visby"",
""RLG"" :  ""Rostock-Laage"",
""CPT"" :  ""Cape Town"",
""JNB"" :  ""Johannesburg"",
""MRU"" :  ""Mauritius"",
""BEW"" :  ""Beira"",
""MPM"" :  ""Maputo"",
""UEL"" :  ""Quelimane"",
""TET"" :  ""Tete"",
""SEZ"" :  ""Mahe"",
""BUQ"" :  ""Bulawayo"",
""VFA"" :  ""Victoria Falls"",
""HRE"" :  ""Harare"",
""BJL"" :  ""Banjul"",
""FUE"" :  ""Fuerteventura"",
""VDE"" :  ""El Hierro"",
""SPC"" :  ""La Palma"",
""LPA"" :  ""Gran Canaria"",
""ACE"" :  ""Lanzarote"",
""TFS"" :  ""Tenerife South"",
""TFN"" :  ""Tenerife North"",
""AGA"" :  ""Agadir"",
""FEZ"" :  ""Fez"",
""ERH"" :  ""Errachidia"",
""OUD"" :  ""Oujda"",
""RBA"" :  ""Rabat"",
""CMN"" :  ""Casablanca"",
""RAK"" :  ""Marrakech"",
""OZZ"" :  ""Ouarzazate"",
""AHU"" :  ""Al Hoceima"",
""TTU"" :  ""Tetuan"",
""TNG"" :  ""Tangier"",
""NKC"" :  ""Nouakschott"",
""SID"" :  ""Sal"",
""BVC"" :  ""Boa Vista"",
""HGA"" :  ""Hargeisa"",
""CAI"" :  ""Cairo"",
""HRG"" :  ""Hurghada"",
""LXR"" :  ""Luxor"",
""MBA"" :  ""Mombasa"",
""KRT"" :  ""Khartoum"",
""ZNZ"" :  ""Zanzibar"",
""TIA"" :  ""Tirana"",
""BOJ"" :  ""Bourgas"",
""PDV"" :  ""Plovdiv"",
""SOF"" :  ""Sofia"",
""VAR"" :  ""Varna"",
""LCA"" :  ""Larnaca"",
""PFO"" :  ""Paphos"",
""DBV"" :  ""Dubrovnik"",
""OSI"" :  ""Osijek"",
""PUY"" :  ""Pula"",
""RJK"" :  ""Rijeka"",
""SPU"" :  ""Split"",
""ZAG"" :  ""Zagreb"",
""ZAD"" :  ""Zadar"",
""ALC"" :  ""Alicante"",
""LEI"" :  ""Almeria"",
""OVD"" :  ""Asturias"",
""BIO"" :  ""Bilbao"",
""BCN"" :  ""Barcelona"",
""LCG"" :  ""La Coruna"",
""GRO"" :  ""Barcelona (Girona)"",
""GRX"" :  ""Granada"",
""IBZ"" :  ""Ibiza"",
""XRY"" :  ""Jerez De La Frontera"",
""MAD"" :  ""Madrid"",
""AGP"" :  ""Malaga"",
""MAH"" :  ""Menorca"",
""RMU"" :  ""Murcia"",
""REU"" :  ""Barcelona (Reus)"",
""EAS"" :  ""San Sebastian"",
""SCQ"" :  ""Santiago De Compostela"",
""VLC"" :  ""Valencia"",
""VLL"" :  ""Valladolid"",
""VIT"" :  ""Vitoria-Gasteiz"",
""VGO"" :  ""Vigo"",
""SDR"" :  ""Santander"",
""ZAZ"" :  ""Zaragoza"",
""SVQ"" :  ""Sevilla"",
""BOD"" :  ""Bordeaux"",
""EGC"" :  ""Bergerac"",
""PIS"" :  ""Poitiers"",
""LIG"" :  ""Limoges"",
""TLS"" :  ""Toulouse"",
""PUF"" :  ""Pau"",
""LDE"" :  ""Lourdes-Tarbes"",
""BVE"" :  ""Brive"",
""BIQ"" :  ""Biarritz"",
""RDZ"" :  ""Rodez"",
""DLE"" :  ""Dole-Jura"",
""ETZ"" :  ""Metz-Nancy"",
""BIA"" :  ""Bastia"",
""CLY"" :  ""Calvi"",
""FSC"" :  ""Figari"",
""AJA"" :  ""Ajaccio"",
""CMF"" :  ""Chambery"",
""CFE"" :  ""Clermont-Ferrand"",
""LYS"" :  ""Lyon"",
""GNB"" :  ""Grenoble"",
""CCF"" :  ""Carcassonne"",
""MRS"" :  ""Marseille"",
""NCE"" :  ""Nice"",
""PGF"" :  ""Perpignan"",
""MPL"" :  ""Montpellier"",
""BZR"" :  ""Beziers"",
""BVA"" :  ""Paris (Beauvais)"",
""XCR"" :  ""Chalons-Vatry"",
""TUF"" :  ""Tours"",
""CDG"" :  ""Paris (Charles de Gaulle)"",
""ORY"" :  ""Paris (Orly)"",
""LIL"" :  ""Lille"",
""BES"" :  ""Brest"",
""DNR"" :  ""Dinard"",
""DOL"" :  ""Deauville"",
""CFR"" :  ""Caen"",
""RNS"" :  ""Rennes"",
""NTE"" :  ""Nantes"",
""MLH"" :  ""Basel-Mulhouse-Freiburg (MLH)"",
""SXB"" :  ""Strasbourg"",
""TLN"" :  ""Toulon-Hyeres"",
""FNI"" :  ""Nimes"",
""VOL"" :  ""Volos"",
""JKH"" :  ""Chios"",
""IOA"" :  ""Ioannina"",
""HER"" :  ""Heraklion"",
""EFL"" :  ""Kefalonia"",
""KLX"" :  ""Kalamata"",
""KGS"" :  ""Kos"",
""AOK"" :  ""Karpathos"",
""CFU"" :  ""Corfu"",
""KVA"" :  ""Kavala"",
""JMK"" :  ""Mikonos"",
""PVK"" :  ""Preveza-Lefkas"",
""RHO"" :  ""Rhodes"",
""GPA"" :  ""Patras"",
""CHQ"" :  ""Chania"",
""JSI"" :  ""Skiathos"",
""SMI"" :  ""Samos"",
""JTR"" :  ""Santorini"",
""SKG"" :  ""Thessaloniki"",
""ZTH"" :  ""Zakinthos"",
""BUD"" :  ""Budapest"",
""DEB"" :  ""Debrecen"",
""CRV"" :  ""Crotone"",
""BRI"" :  ""Bari"",
""PSR"" :  ""Pescara"",
""BDS"" :  ""Brindisi"",
""SUF"" :  ""Lamezia Terme"",
""CTA"" :  ""Catania"",
""LMP"" :  ""Lampedusa"",
""PNL"" :  ""Pantelleria"",
""PMO"" :  ""Palermo"",
""REG"" :  ""Reggio Calabria"",
""TPS"" :  ""Trapani"",
""AHO"" :  ""Alghero"",
""CAG"" :  ""Cagliari"",
""OLB"" :  ""Olbia"",
""MXP"" :  ""Milan (Malpensa)"",
""BGY"" :  ""Milan (Bergamo)"",
""TRN"" :  ""Turin"",
""GOA"" :  ""Genova"",
""LIN"" :  ""Milan (Linate)"",
""PMF"" :  ""Parma"",
""CUF"" :  ""Cuneo"",
""BLQ"" :  ""Bologna"",
""TSF"" :  ""Venice (Treviso)"",
""TRS"" :  ""Trieste"",
""RMI"" :  ""Rimini"",
""VRN"" :  ""Verona"",
""VCE"" :  ""Venice (Marco Polo)"",
""CIA"" :  ""Rome (Ciampino)"",
""FCO"" :  ""Rome (Fiumicino)"",
""NAP"" :  ""Naples"",
""PSA"" :  ""Pisa"",
""FLR"" :  ""Florence"",
""PEG"" :  ""Perugia"",
""LJU"" :  ""Ljubljana"",
""KLV"" :  ""Karlovy Vary"",
""OSR"" :  ""Ostrava"",
""PED"" :  ""Pardubice"",
""PRG"" :  ""Prague"",
""BRQ"" :  ""Brno"",
""TLV"" :  ""Tel Aviv-Yafo (Ben Gurion)"",
""ETM"" :  ""Eilat (Ramon)"",
""SDV"" :  ""Tel Aviv-Yafo (Sde Dov)"",
""MLA"" :  ""Malta"",
""GRZ"" :  ""Graz"",
""INN"" :  ""Innsbruck"",
""SZG"" :  ""Salzburg"",
""VIE"" :  ""Vienna"",
""FAO"" :  ""Faro"",
""TER"" :  ""Terceira Lajes (Azores)"",
""PDL"" :  ""Ponta Delgada (Azores)"",
""OPO"" :  ""Porto"",
""PXO"" :  ""Porto Santo"",
""LIS"" :  ""Lisbon"",
""OMO"" :  ""Mostar"",
""SJJ"" :  ""Sarajevo"",
""BCM"" :  ""Bacau"",
""CND"" :  ""Constanta"",
""CLJ"" :  ""Cluj-Napoca"",
""CRA"" :  ""Craiova"",
""IAS"" :  ""Iasi"",
""OTP"" :  ""Bucharest (Otopeni)"",
""SBZ"" :  ""Sibiu"",
""SUJ"" :  ""Satu Mare"",
""SCV"" :  ""Suceava"",
""TGM"" :  ""Tirgu Mures"",
""TSR"" :  ""Timisoara"",
""GVA"" :  ""Geneva"",
""ZRH"" :  ""Zurich"",
""ESB"" :  ""Ankara"",
""ADA"" :  ""Adana"",
""AYT"" :  ""Antalya"",
""GZT"" :  ""Gaziantep"",
""KYA"" :  ""Konya"",
""MZH"" :  ""Merzifon"",
""VAS"" :  ""Sivas"",
""MLX"" :  ""Malatya"",
""ASR"" :  ""Kayseri"",
""DNZ"" :  ""Denizli Cardak"",
""IST"" :  ""Istanbul (Ataturk)"",
""ADB"" :  ""Izmir"",
""DLM"" :  ""Dalaman"",
""EZS"" :  ""Elazig"",
""DIY"" :  ""Diyarbakir"",
""ERC"" :  ""Erzincan"",
""ERZ"" :  ""Erzurum"",
""TZX"" :  ""Trabzon"",
""VAN"" :  ""Van"",
""BAL"" :  ""Batman"",
""KIV"" :  ""Chisinau"",
""OHD"" :  ""Ohrid"",
""SKP"" :  ""Skopje"",
""GIB"" :  ""Gibraltar"",
""BEG"" :  ""Belgrade"",
""INI"" :  ""Nis"",
""TGD"" :  ""Podgorica"",
""PRN"" :  ""Prishtina"",
""TIV"" :  ""Tivat"",
""BTS"" :  ""Bratislava"",
""KSC"" :  ""Kosice"",
""TAT"" :  ""Poprad-Tatry"",
""LRM"" :  ""La Romana"",
""PUJ"" :  ""Punta Cana"",
""POP"" :  ""Puerto Plata"",
""SDQ"" :  ""Santo Domingo"",
""MBJ"" :  ""Montego Bay"",
""PVR"" :  ""Puerto Vallarta"",
""CUN"" :  ""Cancun"",
""CYO"" :  ""Cayo"",
""SCU"" :  ""Santiago De Cuba"",
""HAV"" :  ""Havana"",
""HOG"" :  ""Holguin"",
""VRA"" :  ""Varadero"",
""RAR"" :  ""Rarotonga"",
""NAN"" :  ""Nadi"",
""TBU"" :  ""Tongatapu"",
""AKL"" :  ""Auckland"",
""CHC"" :  ""Christchurch"",
""ZQN"" :  ""Queenstown"",
""WLG"" :  ""Wellington"",
""KBL"" :  ""Kabul"",
""BAH"" :  ""Bahrain"",
""AHB"" :  ""Abha"",
""BHH"" :  ""Bisha"",
""DMM"" :  ""Dammam"",
""GIZ"" :  ""Jizan"",
""ELQ"" :  ""Gassim"",
""HAS"" :  ""Hail"",
""JED"" :  ""Jeddah"",
""MED"" :  ""Madinah"",
""RUH"" :  ""Riyadh"",
""SHW"" :  ""Sharurah"",
""TUU"" :  ""Tabuk"",
""TIF"" :  ""Taif"",
""YNB"" :  ""Yanbu"",
""SYZ"" :  ""Shiraz"",
""AMM"" :  ""Amman (Queen Alia)"",
""AQJ"" :  ""Aqaba"",
""KWI"" :  ""Kuwait"",
""BEY"" :  ""Beirut"",
""AUH"" :  ""Abu Dhabi"",
""DXB"" :  ""Dubai"",
""RKT"" :  ""Ras Al Khaimah"",
""SHJ"" :  ""Sharjah"",
""MCT"" :  ""Muscat"",
""SLL"" :  ""Salalah"",
""LYP"" :  ""Faisalabad"",
""KHI"" :  ""Karachi"",
""LHE"" :  ""Lahore"",
""MUX"" :  ""Multan"",
""PEW"" :  ""Peshawar"",
""UET"" :  ""Quetta"",
""ISB"" :  ""Islamabad"",
""BSR"" :  ""Basra"",
""DOH"" :  ""Doha"",
""ANU"" :  ""Saint Johns Antigua"",
""BGI"" :  ""Bridgetown"",
""FDF"" :  ""Fort-de-France (Le Lamentin)"",
""PTP"" :  ""Pointe-à-Pitre"",
""ALA"" :  ""Almaty"",
""TSE"" :  ""Astana"",
""FRU"" :  ""Bishkek"",
""OSS"" :  ""Osh"",
""GYD"" :  ""Baku"",
""IKT"" :  ""Irkutsk"",
""UUD"" :  ""Ulan-Ude (Baikal)"",
""KBP"" :  ""Kiev (Borispol)"",
""SIP"" :  ""Simferopol"",
""IEV"" :  ""Kiev (Zhuliany)"",
""LWO"" :  ""Lviv"",
""ODS"" :  ""Odessa"",
""LED"" :  ""St Petersburg"",
""MMK"" :  ""Murmansk"",
""KGD"" :  ""Kaliningrad"",
""MSQ"" :  ""Minsk"",
""KEJ"" :  ""Kemerovo"",
""OMS"" :  ""Omsk"",
""KRR"" :  ""Krasnodar"",
""MCX"" :  ""Makhachkala"",
""MRV"" :  ""Mineralnye Vody"",
""ROV"" :  ""Rostov-on-Don"",
""AER"" :  ""Sochi"",
""ASF"" :  ""Astrakhan"",
""VOG"" :  ""Volgograd"",
""CEK"" :  ""Chelyabinsk"",
""PEE"" :  ""Perm"",
""SGC"" :  ""Surgut"",
""SVX"" :  ""Ekaterinburg"",
""SVO"" :  ""Moscow (Sheremetyevo)"",
""VOZ"" :  ""Voronezh"",
""VKO"" :  ""Moscow (Vnukovo)"",
""SCW"" :  ""Syktyvkar"",
""KZN"" :  ""Kazan"",
""REN"" :  ""Orenburg"",
""UFA"" :  ""Ufa"",
""AMD"" :  ""Ahmedabad"",
""BOM"" :  ""Mumbai"",
""BDQ"" :  ""Vadodara"",
""GOI"" :  ""Goa"",
""IDR"" :  ""Indore"",
""NAG"" :  ""Nagpur"",
""PNQ"" :  ""Pune"",
""RPR"" :  ""Raipur"",
""STV"" :  ""Surat"",
""UDR"" :  ""Udaipur"",
""CMB"" :  ""Colombo"",
""IXA"" :  ""Agartala"",
""IXB"" :  ""Bagdogra"",
""BBI"" :  ""Bhubaneshwar"",
""CCU"" :  ""Calcutta"",
""IMF"" :  ""Imphal"",
""JRH"" :  ""Jorhat"",
""PAT"" :  ""Patna"",
""IXR"" :  ""Birsa Munda"",
""VTZ"" :  ""Vishakhapatnam"",
""CGP"" :  ""Chittagong"",
""DAC"" :  ""Dhaka"",
""HKG"" :  ""Hong Kong"",
""IXD"" :  ""Allahabad"",
""ATQ"" :  ""Amritsar"",
""VNS"" :  ""Varanasi"",
""IXC"" :  ""Chandigarh"",
""DED"" :  ""Dehradun"",
""DEL"" :  ""Delhi"",
""JAI"" :  ""Jaipur"",
""IXJ"" :  ""Jammu"",
""IXL"" :  ""Leh"",
""LKO"" :  ""Lucknow"",
""SXR"" :  ""Srinagar"",
""KTM"" :  ""Kathmandu"",
""BLR"" :  ""Bangalore"",
""VGA"" :  ""Vijayawada"",
""CJB"" :  ""Coimbatore"",
""COK"" :  ""Cochin"",
""CCJ"" :  ""Calicut"",
""HYD"" :  ""Hyderabad"",
""IXM"" :  ""Madurai"",
""IXE"" :  ""Mangalore"",
""MAA"" :  ""Chennai"",
""IXZ"" :  ""Port Blair"",
""RJA"" :  ""Rajahmundry"",
""TIR"" :  ""Tirupati"",
""TRZ"" :  ""Tiruchirappalli"",
""TRV"" :  ""Trivandrum"",
""MLE"" :  ""Male"",
""KBV"" :  ""Krabi"",
""HKT"" :  ""Phuket"",
""MDL"" :  ""Mandalay"",
""RGN"" :  ""Yangon"",
""KUL"" :  ""Kuala Lumpur"",
""SIN"" :  ""Singapore Changi"",
""ASP"" :  ""Alice Springs"",
""BNE"" :  ""Brisbane"",
""OOL"" :  ""Gold Coast"",
""CNS"" :  ""Cairns"",
""ISA"" :  ""Mount Isa"",
""MCY"" :  ""Sunshine Coast"",
""MKY"" :  ""Mackay"",
""PPP"" :  ""Proserpine"",
""ROK"" :  ""Rockhampton"",
""TSV"" :  ""Townsville"",
""ABX"" :  ""Albury"",
""HBA"" :  ""Hobart"",
""LST"" :  ""Launceston"",
""MEL"" :  ""Melbourne"",
""ADL"" :  ""Adelaide"",
""KTA"" :  ""Karratha"",
""KGI"" :  ""Kalgoorlie"",
""PHE"" :  ""Port Hedland"",
""PER"" :  ""Perth"",
""CBR"" :  ""Canberra"",
""CFS"" :  ""Coffs Harbour"",
""SYD"" :  ""Sydney"",
""TMW"" :  ""Tamworth"",
""TSN"" :  ""Binhai"",
""TYN"" :  ""Wusu"",
""CAN"" :  ""Guangzhou"",
""CSX"" :  ""Huanghua"",
""NNG"" :  ""Wuxu"",
""CGO"" :  ""Xinzheng"",
""WUH"" :  ""Tianhe"",
""XIY"" :  ""Xianyang"",
""XMN"" :  ""Gaoqi"",
""KHN"" :  ""Nanchang"",
""NGB"" :  ""Lishe"",
""NKG"" :  ""Lukou"",
""HFE"" :  ""Luogang"",
""CKG"" :  ""Jiangbei"",
""KWE"" :  ""Longdongbao"",
""URC"" :  ""Urumqi"",
""HRB"" :  ""Taiping"",
""DLC"" :  ""Zhoushuizi"",
""HBE"" :  ""Alexandria"",
""BOS"" :  ""Boston"",
""OAK"" :  ""Oakland"",
""SFO"" :  ""San Francisco"",
""LAX"" :  ""Los Angeles"",
""EWR"" :  ""Newark"",
""FLL"" :  ""Fort Lauderdale"",
""MIA"" :  ""Miami"",
""SEA"" :  ""Seattle"",
""PVD"" :  ""Providence"",
""SWF"" :  ""Stewart"",
""IAD"" :  ""Washington"",
""HNL"" :  ""Honolulu"",
""DEN"" :  ""Denver"",
""RSW"" :  ""Fort Myers"",
""JFK"" :  ""New York (JFK)"",
""BDL"" :  ""Bradley"",
""ORD"" :  ""Chicago O'Hare"",
""MSP"" :  ""Minneapolis"",
""LAS"" :  ""Las Vegas"",
""MCO"" :  ""Orlando"",
""BKK"" :  ""Bangkok (Suvarnabhumi)"",
""DPS"" :  ""Denpasar (Bali)"",
""ATH"" :  ""Athens"",
""LPX"" :  ""Liepaja"",
""RIX"" :  ""Riga"",
""KUN"" :  ""Kaunas"",
""PLQ"" :  ""Klaipeda-Palanga"",
""VNO"" :  ""Vilnius"",
""EVN"" :  ""Yerevan"",
""LWN"" :  ""Gyumri"",
""ASM"" :  ""Asmara"",
""BUS"" :  ""Batumi"",
""KUT"" :  ""Kutaisi"",
""TBS"" :  ""Tbilisi"",
""FMM"" :  ""Memmingen"",
""EBL"" :  ""Erbil"",
""EMD"" :  ""Emerald"",
""PMI"" :  ""Palma Mallorca"",
""DRW"" :  ""Darwin"",
""AYQ"" :  ""Ayers Rock"",
""DME"" :  ""Moscow (Domodedovo)"",
""SYX"" :  ""Sanya"",
""HVB"" :  ""Hervey Bay"",
""BSL"" :  ""Basel-Mulhouse-Freiburg (BSL)"",
""JHG"" :  ""Jinghong"",
""SSH"" :  ""Sharm El Sheikh"",
""NBO"" :  ""Nairobi"",
""OVB"" :  ""Novosibirsk (Tolmachevo)"",
""FNC"" :  ""Madeira (Funchal)"",
""TJM"" :  ""Tyumen"",
""KUF"" :  ""Samara"",
""HAK"" :  ""Meilan"",
""BGW"" :  ""Baghdad"",
""SHE"" :  ""Shenyang"",
""FKB"" :  ""Karlsruhe-Baden Baden"",
""RMF"" :  ""Marsa Alam"",
""NRN"" :  ""Dusseldorf (Weeze)"",
""BDB"" :  ""Bundaberg"",
""BNK"" :  ""Ballina"",
""TOF"" :  ""Tomsk"",
""AOI"" :  ""Ancona"",
""BJV"" :  ""Bodrum"",
""SAW"" :  ""Istanbul (Sabiha Gokcen)"",
""BME"" :  ""Broome"",
""NTL"" :  ""Newcastle Williamtown"",
""KLU"" :  ""Klagenfurt"",
""HFT"" :  ""Hammerfest"",
""HVG"" :  ""Honningsvag"",
""MEH"" :  ""Mehamn"",
""VDS"" :  ""Vadso"",
""IKA"" :  ""Tehran"",
""MHD"" :  ""Mashhad"",
""HOV"" :  ""Orsta-Volda"",
""BVG"" :  ""Berlevag"",
""ARH"" :  ""Arkhangelsk"",
""KJA"" :  ""Krasnoyarsk"",
""CGQ"" :  ""Changchun"",
""FDE"" :  ""Forde"",
""HDF"" :  ""Heringsdorf"",
""DSA"" :  ""Doncaster-Sheffield"",
""VLY"" :  ""Angelsey"",
""CFN"" :  ""Donegal"",
""LKN"" :  ""Leknes"",
""OSY"" :  ""Namsos"",
""MQN"" :  ""Mo i Rana"",
""RVK"" :  ""Roervik"",
""RET"" :  ""Rost (Lofoten)"",
""SDN"" :  ""Sandane"",
""SOG"" :  ""Sogndal"",
""SVJ"" :  ""Svolvaer"",
""SOJ"" :  ""Sorkjosen"",
""VAW"" :  ""Vardoe"",
""BZG"" :  ""Bydgoszcz"",
""LCJ"" :  ""Lodz"",
""OSD"" :  ""Ostersund"",
""KSD"" :  ""Karlstad"",
""GMZ"" :  ""Gomera"",
""VIL"" :  ""Dakhla"",
""ESU"" :  ""Mogador-Essadouira"",
""EUN"" :  ""El Aaiun"",
""NDR"" :  ""Nador"",
""ATZ"" :  ""Asyut"",
""ECN"" :  ""Ercan (Nicosia)"",
""BNX"" :  ""Banja Luka"",
""KSY"" :  ""Kars"",
""KCM"" :  ""Kahramanmaras"",
""AJI"" :  ""Agri"",
""EDO"" :  ""Edremit"",
""SZF"" :  ""Samsun-Carsamba"",
""CCC"" :  ""Cayo Coco"",
""AJF"" :  ""Al-Jawf"",
""WAE"" :  ""Wadi al-Dawasir"",
""LRR"" :  ""Larestan"",
""ISU"" :  ""Sulaymaniyah"",
""OZH"" :  ""Zaporozhye"",
""HRK"" :  ""Kharkov"",
""PES"" :  ""Petrozavodsk"",
""GRV"" :  ""Grozny"",
""NAL"" :  ""Nalchik"",
""OGZ"" :  ""Vladikavkaz-Beslan"",
""KVX"" :  ""Kirov (Pobedilovo)"",
""CSY"" :  ""Cheboksary"",
""HBX"" :  ""Hubballi"",
""GAU"" :  ""Guwahati"",
""DMU"" :  ""Dimapur"",
""HTI"" :  ""Hamilton Island"",
""GET"" :  ""Geraldton"",
""GLT"" :  ""Gladstone"",
""MQL"" :  ""Mildura"",
""ZNE"" :  ""Newman"",
""PQQ"" :  ""Port Macquarie"",
""HET"" :  ""Hohhot"",
""BAV"" :  ""Baotou"",
""NNY"" :  ""Nanyang"",
""XNN"" :  ""Xining"",
""WUX"" :  ""Wuxi"",
""WNZ"" :  ""Wenzhou"",
""YNJ"" :  ""Yanji"",
""LHW"" :  ""Lanzhou"",
""DIB"" :  ""Dibrugarh"",
""LRH"" :  ""La Rochelle"",
""GOP"" :  ""Gorakhpur"",
""MQM"" :  ""Mardin"",
""SKT"" :  ""Sialkot"",
""AOE"" :  ""Eskisehir"",
""MSR"" :  ""Mus"",
""NOP"" :  ""Sinop"",
""TEQ"" :  ""Tekirdag"",
""BER"" :  ""Berlin (Brandenburg)"",
""TZL"" :  ""Tuzla"",
""NBC"" :  ""Nizhnekamsk"",
""ULV"" :  ""Ulyanovsk"",
""NBE"" :  ""Enfidha"",
""HTY"" :  ""Hatay"",
""NJF"" :  ""Najaf"",
""WMI"" :  ""Warsaw (Modlin)"",
""GZP"" :  ""Gazipasa"",
""LUZ"" :  ""Lublin"",
""KZR"" :  ""Zafer"",
""GNY"" :  ""Sanliurfa"",
""HMB"" :  ""Sohag"",
""DWC"" :  ""Dubai (Al Maktoum)"",
""CIY"" :  ""Comiso"",
""CDT"" :  ""Castellon"",
""IGT"" :  ""Magas"",
""OGU"" :  ""Ordu-Giresun"",
""JIQ"" :  ""Qianjiang Wulingshan"",
""NBS"" :  ""Changbaishan"",
""AVA"" :  ""Anshun Huangguoshu"",
""TCR"" :  ""Tuticorin"",
""BGG"" :  ""Bingol"",
""OHS"" :  ""Sohar"",
""WGN"" :  ""Wugang"",
""DSS"" :  ""Dakar"",
""OZG"" :  ""Zagora"",
""CNN"" :  ""Kannur Intl"",
""RKV"" :  ""Reykjavik (Domestic)"",
""VXE"" :  ""Sao Pedro (Sao Vicente)"",
""JRO"" :  ""Kilimanjaro"",
""DUD"" :  ""Dunedin"",
""ABT"" :  ""Al Baha"",
""UVF"" :  ""Hewandorra"",
""AUA"" :  ""Oranjestad"",
""TAB"" :  ""Scarborough"",
""CGK"" :  ""Jakarta (Soekarno-Hatta)"",
""SZX"" :  ""Shenzhen"",
""SKN"" :  ""Stokmarknes"",
""RAI"" :  ""Praia"",
""CNJ"" :  ""Cloncurry""
}";
        #endregion
    }
}