﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DreamTravel.FlightProviderData.Repository.Airports.PreCalculation
{
    public partial class AirportDataSource
    {
        public static Dictionary<string, List<string>> GetCountryToCodesMap()
        {
            var placeToAirportsDataModels = JsonConvert.DeserializeObject<Dictionary<string, PlaceToAirportsDataModel>>(Place2CodesMap);

            var placeToCodesMap = placeToAirportsDataModels.Values
                .ToDictionary(k => k.Name, v => v.Ports.Split("_").ToList());


            return placeToCodesMap;
        }


        #region place2Codes json
        // 2019-09-10 07:25:01
        private const string Place2CodesMap = @"
{ 
  ""LON_ALL"" :  { ""name"" : ""London"",""ports"":""LGW_STN_LTN_LCY_LHR_SEN_BQH""},
  ""STO_ALL"" :  { ""name"" : ""Stockholm"",""ports"":""ARN_BMA_NYO_VST""},
  ""BAR_ALL"" :  { ""name"" : ""Barcelona"",""ports"":""BCN_GRO_REU""},
  ""DUS_ALL"" :  { ""name"" : ""Duesseldorf"",""ports"":""DUS_NRN_MGL""},
  ""IEV_ALL"" :  { ""name"" : ""Kiev"",""ports"":""IEV_KBP""},
  ""MIL_ALL"" :  { ""name"" : ""Milan"",""ports"":""MXP_BGY_LIN""},
  ""MOW_ALL"" :  { ""name"" : ""Moscow"",""ports"":""DME_SVO_VKO_BKA""},
  ""OSL_ALL"" :  { ""name"" : ""Oslo"",""ports"":""OSL_TRF_RYG""},
  ""HAM_ALL"" :  { ""name"" : ""Hamburg"",""ports"":""HAM_LBC""},
  ""PAR_ALL"" :  { ""name"" : ""Paris"",""ports"":""CDG_ORY_BVA_XCR_LBG""},
  ""BEL_ALL"" :  { ""name"" : ""Belfast"",""ports"":""BFS_BHD""},
  ""BER_ALL"" :  { ""name"" : ""Berlin"",""ports"":""SXF_TXL""},
  ""BRU_ALL"" :  { ""name"" : ""Brussels"",""ports"":""BRU_CRL""},
  ""VEN_ALL"" :  { ""name"" : ""Venice"",""ports"":""VCE_TSF""},
  ""WAR_ALL"" :  { ""name"" : ""Warsaw"",""ports"":""WAW_WMI""},
  ""BUH_ALL"" :  { ""name"" : ""Bucharest"",""ports"":""OTP_BBU""},
  ""ROM_ALL"" :  { ""name"" : ""Rome"",""ports"":""FCO_CIA""},
  ""FRA_ALL"" :  { ""name"" : ""Frankfurt"",""ports"":""FRA_HHN""},
  ""IST_ALL"" :  { ""name"" : ""Istanbul"",""ports"":""IST_SAW""},
  ""REK_ALL"" :  { ""name"" : ""Reykjavik"",""ports"":""RKV_KEF""},
  ""TLV_ALL"" :  { ""name"" : ""Tel Aviv-Yafo"",""ports"":""TLV_SDV""},
  ""GLA_ALL"" :  { ""name"" : ""Glasgow"",""ports"":""PIK_GLA""},
  ""DXW_ALL"" :  { ""name"" : ""Dubai"",""ports"":""DXB_DWC""},
  ""GOT_ALL"" :  { ""name"" : ""Goteborg"",""ports"":""GOT_GSE""},
  ""CZNE"" :  { ""name"" : ""Czech and nearby airports"",""ports"":""PRG_BRQ_KLV_PED_OSR_BTS_KSC_LEJ_CSO_KRK_KTW_WRO_LNZ_VIE_DRS_NUE_BUD_MUC_SZG_SXF_TXL""},
  ""CZMO"" :  { ""name"" : ""Moravia"",""ports"":""BRQ_OSR_KTW_BTS_PRG_VIE_BUD""},
  ""CZLU"" :  { ""name"" : ""Czech and wider area airports"",""ports"":""BTS_KSC_BRQ_BUD_CSO_DRS_KTW_KRK_LEJ_LNZ_FMM_MUC_NUE_OSR_PRG_KLV_PED_SXF_SZG_TXL_VIE_WRO""},
  ""ESDA"" :  { ""name"" : ""Andalusia (ES)"",""ports"":""LEI_ODB_GRX_XRY_AGP_SVQ_OZP""},
  ""ESCT"" :  { ""name"" : ""Catalonia (ES)"",""ports"":""BCN_GRO_REU_ILD""},
  ""DEBW"" :  { ""name"" : ""Baden-Württemberg (DE)"",""ports"":""FKB_FDH_LHA_MHG_STR""},
  ""DEBA"" :  { ""name"" : ""Bavaria (DE)"",""ports"":""IGS_HOQ_FMM_MUC_BYU_AGB_GHF_ZCD_NUE""},
  ""DEHE"" :  { ""name"" : ""Hesse (DE)"",""ports"":""KSF_FRA_FRZ_WIE""},
  ""DELS"" :  { ""name"" : ""Lower Saxony (DE)"",""ports"":""HAJ _BWE_BMK_EME_ZCN_NDZ""},
  ""DEMV"" :  { ""name"" : ""Mecklenburg-Vorpommern (DE)"",""ports"":""FNB_HDF_RLG_REB""},
  ""DERW"" :  { ""name"" : ""North Rhine-Westphalia (DE)"",""ports"":""FMO_AAH_MGL_DUS_DTM_CGN_GKE_SGE_PAD_NRN_QOE_ZPQ""},
  ""DERP"" :  { ""name"" : ""Rhineland-Palatinate (DE)"",""ports"":""HHN_ZQW_SPM_RMS""},
  ""DESR"" :  { ""name"" : ""Saarland (DE)"",""ports"":""SCN""},
  ""DESA"" :  { ""name"" : ""Saxony-Anhalt (DE)"",""ports"":""LEJ_DRS_CSO""},
  ""DESH"" :  { ""name"" : ""Schleswig-Holstein (DE)"",""ports"":""KEL_LBC_HEI_HGL_GWT_WBG""},
  ""DETH"" :  { ""name"" : ""Thuringia (DE)"",""ports"":""AOC_ERF""},
  ""ADRIATIC"" :  { ""name"" : ""Adriatic coast"",""ports"":""PUY_RJK_SPU_DBV_ZAD""},
  ""ARABIA"" :  { ""name"" : ""Arabian peninsula"",""ports"":""DXB_HAS_AHB_TUU_KWI_MED_ELQ_SLL_TIF_SHJ_JED_DMM_YNB_MCT_RUH""},
  ""ATLANTIC"" :  { ""name"" : ""Atlantic"",""ports"":""GMZ_VXE_FNC_SID_RAI_TFN_BVC_FUE_VDE_LPA_ACE_PXO_TFS_PDL""},
  ""AZORES"" :  { ""name"" : ""Azores"",""ports"":""PDL_TER""},
  ""MADEIRA"" :  { ""name"" : ""Madeira"",""ports"":""FNC_PXO""},
  ""CANARIES"" :  { ""name"" : ""Canary Islands"",""ports"":""SPC_FUE_VDE_LPA_ACE_TFS_GMZ_TFN""},
  ""CAPEVERDE"" :  { ""name"" : ""Cape Verde Islands"",""ports"":""SID_RAI_BVC""},
  ""CORSICA"" :  { ""name"" : ""Corsica"",""ports"":""AJA_BIA_FSC_PRP_CLY""},
  ""GR_CRETE"" :  { ""name"" : ""Greece: Crete"",""ports"":""CHQ_HER_JSH""},
  ""SARDINIA"" :  { ""name"" : ""Sardinia"",""ports"":""CAG_AHO_OLB""},
  ""SICILY"" :  { ""name"" : ""Sicily"",""ports"":""CTA_CIY_PMO_PNL_TPS""},
  ""ENGLAND"" :  { ""name"" : ""England"",""ports"":""SEN_NQY_EXT_JER_BRS_GCI_BOH_NWI_NCL_STN_MME_LTN_MAN_LHR_LPL_LGW_LBA_CWL_HUY_EMA_DSA_BLK_BHX_SOU""},
  ""GR_ISLANDS"" :  { ""name"" : ""Greece: Islands"",""ports"":""CFU_KGS_JMK_HER_PVK_JTR_JKH_SMI_CHQ_MJT_JSI_ZTH_EFL_RHO""},
  ""CHANNEL_ISLANDS"" :  { ""name"" : ""Channel Islands"",""ports"":""GCI_JER_ACI""},
  ""MEDITERRANEAN_CO"" :  { ""name"" : ""Mediterranean coast"",""ports"":""ALG_ORN_HBE_AHU_NDR_TTU_DJE_MIR_NBE_TUN_AGP_ALC_BCN_GRO_LEI_REU_VLC_XRY_GIB_AVN_BZR_CCF_FNI_MPL_MRS_NCE_PGF_TLN_ATH_GPA_KLX_KVA_SKG_VOL_DBV_SPU_ZAD_PUY_RJK_ZAG_AOI_BDS_PSR_REG_RMI_TRS_TSF_VCE_TLV_AYT_""},
  ""MEDITERRANEAN_IS"" :  { ""name"" : ""Mediterranean islands"",""ports"":""ECN_LCA_PFO_IBZ_MAH_PMI_AJA_BIA_FSC_CFU_CHQ_EFL_HER_JKH_JMK_JSI_JTR_KGS_MJT_PVK_RHO_SMI_ZTH_AHO_CAG_OLB_CTA_LMP_PMO_TPS_MLA""},
  ""SCANDINAVIA"" :  { ""name"" : ""Scandinavia"",""ports"":""ANX_KKN_HAU_OSL_SVG_BDU_LKL_KRS_TRD_BOO_LYR_AES_MOL_TRF_ALF_EVE_TOS_BGO_RYG_LLA_KSD_KLR_JKG_BMA_GSE_NYO_VXO_VBY_AGH_VST_UME_SFT_NRK_MMX_ARN_GOT_BLL_AAR_KRP_AAL_CPH""},
  ""SCOTLAND"" :  { ""name"" : ""Scotland"",""ports"":""EDI_WIC_BEB_TRE_LSI_PIK_BRR_TRE_SYY_KOI_ILY_GLA_CAL_WIC_ABZ_SYY_LSI_INV""},
  ""ESBA"" :  { ""name"" : ""Balearic Islands"",""ports"":""PMI_IBZ_MAH""},
  ""ESGA"" :  { ""name"" : ""Galicia (ES)"",""ports"":""LCG_SCQ_VGO""},
  ""ESBS"" :  { ""name"" : ""Basque (ES)"",""ports"":""BIO_EAS""},
  ""BALTIC"" :  { ""name"" : ""Baltic countries"",""ports"":""TLL_TAY_KUN_PLQ_VNO_RIX""},
  ""ALPS"" :  { ""name"" : ""Alps mountains"",""ports"":""SZG_INN_GRZ_FMM_MUC_MXP_BGY_LIN_TRN_GVA_ZRH_BRN_GNB_CMF""},
  ""EUMAJOR"" :  { ""name"" : ""Major European airports"",""ports"":""BCN_LGW_DUB_STN_OSL_DUS_PMI_LTN_MAN_AMS_TXL_FCO_ORY_ATH_WAW_CPH_BGY_AGP_MXP_SAW_CGN_EDI_CDG_HAM_BHX_AYT_MAD_MUC_PRG_BUD_STR_SXF_ALC_BRU_GLA_CRL_VIE_BGO_LPA_LYS_ARN_GVA_OTP_FRA_BRQ_BTS""},
  ""TUSCANY"" :  { ""name"" : ""Tuscany"",""ports"":""PSA_FLR""},
  ""NYC_ALL"" :  { ""name"" : ""New York"",""ports"":""JFK_LGA_EWR""},
  ""IT_NORTH"" :  { ""name"" : ""northern Italy"",""ports"":""BLQ_FRL_PMF_RAN_RMI_TSF_VCE_VRN_ALL_GOA_BGY_MXP_LIN_CUF_TRN""},
  ""IT_SOUTH"" :  { ""name"" : ""southern Italy"",""ports"":""BRI_BDS_FOG_CRV_SUF_REG_NAP_QSR""},
  ""IT_CENTRAL"" :  { ""name"" : ""central Italy"",""ports"":""AOI_FLR_PSA_PSR_FCO_CIA_PEG""},
  ""PL_CENTRAL"" :  { ""name"" : ""central Poland"",""ports"":""WAW_WMI_LCJ_RDO""},
  ""PL_EAST"" :  { ""name"" : ""eastern Poland"",""ports"":""RZE_LUZ""},
  ""PL_WEST"" :  { ""name"" : ""western Poland"",""ports"":""SZZ_POZ_IEG""},
  ""PL_SOUTH"" :  { ""name"" : ""southern Poland"",""ports"":""WRO_KTW_KRK""},
  ""PL_NORTH"" :  { ""name"" : ""northern Poland"",""ports"":""GDN_BZG_SZY""},
  ""VAL_ALL"" :  { ""name"" : ""Valencie"",""ports"":""VLC_ALC_CDT""},
  ""TUR_ALL"" :  { ""name"" : ""Turin"",""ports"":""TRN_CUF""},
  ""GR_ATTICA"" :  { ""name"" : ""Greece: Attica"",""ports"":""ATH_KIT""},
  ""GR_IONIAN"" :  { ""name"" : ""Greece: Ionian islands"",""ports"":""CFU_EFL_ZTH""},
  ""GR_EPIRUS"" :  { ""name"" : ""Greece: Epirus"",""ports"":""IOA_PVK""},
  ""GR_NORTHAEGEAN"" :  { ""name"" : ""Greece: North Aegean"",""ports"":""LXS_MJT_SMI_JKH_JIK""},
  ""GR_SOUTHAEGEAN"" :  { ""name"" : ""Greece: South Aegean"",""ports"":""KGS_RHO_JTY_JKL_AOK_MLO_JMK_JNX_PAS_JTR_JSY""},
  ""GR_THESSALY"" :  { ""name"" : ""Greece: Thessaly"",""ports"":""JSI_VOL""},
  ""BKK_ALL"" :  { ""name"" : ""Bangkok"",""ports"":""DMK_BKK""},
  ""FRNO"" :  { ""name"" : ""Normandy"",""ports"":""DOL_CFR""},
  ""FRBR"" :  { ""name"" : ""Brittany"",""ports"":""NTE_DNR_RNS_LRT""},
  ""BALKANS"" :  { ""name"" : ""Balkans"",""ports"":""TIA_SJJ_TZL_TGD_TIV_OHD_SKP_BEG_INI_PRN""},
  ""SKNE"" :  { ""name"" : ""Slovakia and nearby airports"",""ports"":""BTS_KSC_TAT_BRQ_OSR_VIE_BUD_DEB_KRK_KTW""},
  ""NO_CENTRAL"" :  { ""name"" : ""central Norway"",""ports"":""OSY_RRS_RVK_TRD""},
  ""NO_SOUTH"" :  { ""name"" : ""southern Norway"",""ports"":""KRS_OSL_RYG_SKE_TRF_VDB""},
  ""NO_WEST"" :  { ""name"" : ""western Norway"",""ports"":""AES_BGO_FDE_FRO_HOV_KSU_MOL_SDN_SOG_SVG""},
  ""NO_NORTH"" :  { ""name"" : ""northern Norway"",""ports"":""ALF_ANX_BDU_BJF_BNN_BOO_BVG_EVE_HAA_HFT_HVG_KKN_LKL_LKN_MEH_MJF_MQN_NVK_RET_SKN_SOJ_SSJ_SVJ_TOS_VAW_VDS""},
  ""RIV_FR"" :  { ""name"" : ""French Riviera (Côte d'Azur)"",""ports"":""NCE_TLN""},
  ""RIV_IT"" :  { ""name"" : ""Italian Riviera (Ligurian Coast)"",""ports"":""GOA_PSA""},
  ""TYO_ALL"" :  { ""name"" : ""Tokyo"",""ports"":""HND_NRT""},
  ""MEL_ALL"" :  { ""name"" : ""Melbourne"",""ports"":""MEL_AVV""},
  ""JKT_ALL"" :  { ""name"" : ""Jakarta"",""ports"":""HLP_CGK""},
  ""HUNE"" :  { ""name"" : ""Hungary and nearby airports"",""ports"":""BUD_DEB_SOB_BTS_KSC_TAT_VIE_GRZ_LJU_OSI_ZAG_TSR_BRQ""},
  ""PERSGULF"" :  { ""name"" : ""Persian Gulf"",""ports"":""KWI_DMM_HOF_BAH_DOH_AUH_DWC_DXB_SHJ_RKT_OHS_MCT_BND""},
  ""REDSEA"" :  { ""name"" : ""Red Sea Coast"",""ports"":""ETH_VDA_SSH_HRG_JIB_GIZ_JED_YNB""},
  ""CARIBBEAN"" :  { ""name"" : ""Caribbean"",""ports"":""HAV_VRA_CYO_HOG_MBJ_SDQ_LRM_PUJ_POP_PTP_FDF_CUN_BGI""},
  ""AE"" :  { ""name"" : ""United Arab Emirates"",""ports"":""AUH_DXB_RKT_SHJ_DWC""},
  ""AF"" :  { ""name"" : ""Afghanistan"",""ports"":""KBL""},
  ""AG"" :  { ""name"" : ""Antigua and Barbuda"",""ports"":""ANU""},
  ""AL"" :  { ""name"" : ""Albania"",""ports"":""TIA""},
  ""AM"" :  { ""name"" : ""Armenia"",""ports"":""EVN_LWN""},
  ""AT"" :  { ""name"" : ""Austria"",""ports"":""GRZ_INN_LNZ_SZG_VIE_KLU""},
  ""AU"" :  { ""name"" : ""Australia"",""ports"":""ASP_BNE_OOL_CNS_ISA_MCY_MKY_PPP_ROK_TSV_ABX_HBA_LST_MEL_ADL_KTA_KGI_PHE_PER_CBR_CFS_SYD_TMW_EMD_DRW_AYQ_HVB_BDB_BNK_BME_NTL_HTI_GET_GLT_MQL_ZNE_PQQ""},
  ""AW"" :  { ""name"" : ""Aruba"",""ports"":""AUA""},
  ""AZ"" :  { ""name"" : ""Azerbaijan"",""ports"":""GYD""},
  ""BA"" :  { ""name"" : ""Bosnia and Herzegovina"",""ports"":""OMO_SJJ_BNX_TZL""},
  ""BB"" :  { ""name"" : ""Barbados"",""ports"":""BGI""},
  ""BD"" :  { ""name"" : ""Bangladesh"",""ports"":""CGP_DAC""},
  ""BE"" :  { ""name"" : ""Belgium"",""ports"":""ANR_BRU_CRL_LGG_OST""},
  ""BG"" :  { ""name"" : ""Bulgaria"",""ports"":""BOJ_PDV_SOF_VAR""},
  ""BH"" :  { ""name"" : ""Bahrain"",""ports"":""BAH""},
  ""BR"" :  { ""name"" : ""Brazil"",""ports"":""FOR_REC""},
  ""BY"" :  { ""name"" : ""Belarus"",""ports"":""MSQ""},
  ""CA"" :  { ""name"" : ""Canada"",""ports"":""YUL_YYZ""},
  ""CH"" :  { ""name"" : ""Switzerland"",""ports"":""GVA_ZRH_BSL""},
  ""CK"" :  { ""name"" : ""Cook Islands"",""ports"":""RAR""},
  ""CN"" :  { ""name"" : ""China"",""ports"":""TSN_CAN_CSX_NNG_CGO_WUH_XIY_KHN_NGB_NKG_HFE_KWE_URC_HRB_DLC_SYX_JHG_HAK_SHE_CGQ_HET_WUX_WNZ_YNJ_LHW_JIQ_NBS_AVA_WGN""},
  ""CU"" :  { ""name"" : ""Cuba"",""ports"":""CYO_SCU_HAV_HOG_VRA_CCC""},
  ""CV"" :  { ""name"" : ""Cape Verde"",""ports"":""SID_BVC_MMO_SNE_VXE_RAI_SFL""},
  ""CY"" :  { ""name"" : ""Cyprus"",""ports"":""LCA_PFO_ECN""},
  ""CZ"" :  { ""name"" : ""Czech Republic"",""ports"":""KLV_OSR_PED_PRG_BRQ""},
  ""DE"" :  { ""name"" : ""Germany"",""ports"":""SXF_DRS_ERF_FRA_FMO_HAM_CGN_DUS_MUC_NUE_LEJ_SCN_STR_TXL_HAJ_BRE_HHN_PAD_DTM_FDH_GWT_RLG_FMM_FKB_NRN_HDF_BER""},
  ""DK"" :  { ""name"" : ""Denmark"",""ports"":""AAR_BLL_CPH_AAL""},
  ""DO"" :  { ""name"" : ""Dominican Republic"",""ports"":""LRM_PUJ_POP_SDQ""},
  ""DZ"" :  { ""name"" : ""Algeria"",""ports"":""BJA_ALG_CZL_TLM_ORN""},
  ""EE"" :  { ""name"" : ""Estonia"",""ports"":""TLL""},
  ""EG"" :  { ""name"" : ""Egypt"",""ports"":""CAI_HRG_LXR_HBE_SSH_RMF_ATZ_HMB""},
  ""EH"" :  { ""name"" : ""Western Sahara"",""ports"":""VIL_EUN""},
  ""ER"" :  { ""name"" : ""Eritrea"",""ports"":""ASM""},
  ""ES"" :  { ""name"" : ""Spain"",""ports"":""FUE_VDE_SPC_LPA_ACE_TFS_TFN_ALC_LEI_OVD_BIO_BCN_LCG_GRO_GRX_IBZ_XRY_MAD_AGP_MAH_RMU_REU_EAS_SCQ_VLC_VLL_VIT_VGO_SDR_ZAZ_SVQ_PMI_GMZ_CDT""},
  ""FI"" :  { ""name"" : ""Finland"",""ports"":""HEL_KTT_LPP_OUL_RVN_TMP_TKU""},
  ""FJ"" :  { ""name"" : ""Fiji"",""ports"":""NAN""},
  ""FO"" :  { ""name"" : ""Faroe Islands"",""ports"":""FAE""},
  ""FR"" :  { ""name"" : ""France"",""ports"":""BOD_EGC_PIS_LIG_TLS_PUF_LDE_BVE_BIQ_RDZ_DLE_ETZ_BIA_CLY_FSC_AJA_CMF_CFE_LYS_GNB_CCF_MRS_NCE_PGF_MPL_BZR_AVN_BVA_XCR_TUF_CDG_ORY_LIL_BES_DNR_DOL_CFR_RNS_NTE_MLH_SXB_TLN_FNI_LRH""},
  ""GB"" :  { ""name"" : ""United Kingdom"",""ports"":""BFS_BHD_LDY_BHX_MAN_NQY_CWL_BRS_LPL_LTN_BOH_SOU_LGW_LCY_LHR_SEN_HUY_LBA_NCL_MME_EMA_WIC_ABZ_INV_GLA_EDI_PIK_NWI_STN_EXT_DSA_VLY""},
  ""GE"" :  { ""name"" : ""Georgia"",""ports"":""BUS_KUT_TBS""},
  ""GG"" :  { ""name"" : ""Guernsey"",""ports"":""GCI""},
  ""GI"" :  { ""name"" : ""Gibraltar"",""ports"":""GIB""},
  ""GM"" :  { ""name"" : ""Gambia"",""ports"":""BJL""},
  ""GP"" :  { ""name"" : ""Guadeloupe"",""ports"":""PTP""},
  ""GR"" :  { ""name"" : ""Greece"",""ports"":""VOL_JKH_IOA_HER_EFL_KLX_KGS_AOK_CFU_KVA_JMK_MJT_PVK_RHO_GPA_CHQ_JSI_SMI_JTR_SKG_ZTH_ATH""},
  ""HK"" :  { ""name"" : ""Hong Kong"",""ports"":""HKG""},
  ""HR"" :  { ""name"" : ""Croatia"",""ports"":""DBV_OSI_PUY_RJK_SPU_ZAG_ZAD_BWK""},
  ""HU"" :  { ""name"" : ""Hungary"",""ports"":""BUD_DEB""},
  ""ID"" :  { ""name"" : ""Indonesia"",""ports"":""DPS""},
  ""IE"" :  { ""name"" : ""Ireland"",""ports"":""ORK_DUB_NOC_KIR_SNN_CFN""},
  ""IL"" :  { ""name"" : ""Israel"",""ports"":""TLV_ETM_SDV""},
  ""IM"" :  { ""name"" : ""Isle of Man"",""ports"":""IOM""},
  ""IN"" :  { ""name"" : ""India"",""ports"":""AMD_BOM_BDQ_GOI_IDR_NAG_PNQ_RPR_STV_UDR_IXA_IXB_BBI_CCU_IMF_JRH_PAT_IXR_VTZ_IXD_ATQ_VNS_IXC_DED_DEL_JAI_IXJ_IXL_LKO_SXR_BLR_VGA_CJB_COK_CCJ_HYD_IXM_IXE_MAA_IXZ_RJA_TIR_TRZ_TRV_HBX_GAU_DMU_DIB_GOP_TCR_CNN""},
  ""IQ"" :  { ""name"" : ""Iraq"",""ports"":""BSR_EBL_BGW_ISU_NJF""},
  ""IR"" :  { ""name"" : ""Iran"",""ports"":""SYZ_IKA_MHD_LRR""},
  ""IS"" :  { ""name"" : ""Iceland"",""ports"":""IFJ_KEF""},
  ""IT"" :  { ""name"" : ""Italy"",""ports"":""CRV_BRI_PSR_BDS_SUF_CTA_LMP_PNL_PMO_REG_TPS_AHO_CAG_OLB_MXP_BGY_TRN_GOA_LIN_PMF_CUF_BLQ_TSF_TRS_RMI_VRN_VCE_CIA_FCO_NAP_PSA_FLR_PEG_AOI_CIY""},
  ""JE"" :  { ""name"" : ""Jersey"",""ports"":""JER""},
  ""JM"" :  { ""name"" : ""Jamaica"",""ports"":""MBJ""},
  ""JO"" :  { ""name"" : ""Jordan"",""ports"":""AMM_AQJ""},
  ""KE"" :  { ""name"" : ""Kenya"",""ports"":""MBA_NBO""},
  ""KG"" :  { ""name"" : ""Kyrgyzstan"",""ports"":""FRU_OSS""},
  ""KW"" :  { ""name"" : ""Kuwait"",""ports"":""KWI""},
  ""KZ"" :  { ""name"" : ""Kazakhstan"",""ports"":""ALA_TSE""},
  ""LB"" :  { ""name"" : ""Lebanon"",""ports"":""BEY""},
  ""LK"" :  { ""name"" : ""Sri Lanka"",""ports"":""CMB""},
  ""LT"" :  { ""name"" : ""Lithuania"",""ports"":""KUN_PLQ_VNO""},
  ""LU"" :  { ""name"" : ""Luxembourg"",""ports"":""LUX""},
  ""LV"" :  { ""name"" : ""Latvia"",""ports"":""LPX_RIX""},
  ""MA"" :  { ""name"" : ""Morocco"",""ports"":""AGA_FEZ_ERH_OUD_RBA_CMN_RAK_OZZ_AHU_TTU_TNG_ESU_NDR_OZG""},
  ""MD"" :  { ""name"" : ""Moldova"",""ports"":""KIV""},
  ""ME"" :  { ""name"" : ""Montenegro"",""ports"":""TGD_TIV""},
  ""MK"" :  { ""name"" : ""Macedonia"",""ports"":""OHD_SKP""},
  ""MM"" :  { ""name"" : ""Myanmar"",""ports"":""MDL_RGN""},
  ""MQ"" :  { ""name"" : ""Martinique"",""ports"":""FDF""},
  ""MR"" :  { ""name"" : ""Mauritania"",""ports"":""NKC""},
  ""MT"" :  { ""name"" : ""Malta"",""ports"":""MLA""},
  ""MU"" :  { ""name"" : ""Mauritius"",""ports"":""MRU""},
  ""MV"" :  { ""name"" : ""Maldives"",""ports"":""MLE""},
  ""MX"" :  { ""name"" : ""Mexico"",""ports"":""PVR_CUN""},
  ""MY"" :  { ""name"" : ""Malaysia"",""ports"":""KUL""},
  ""MZ"" :  { ""name"" : ""Mozambique"",""ports"":""BEW_MPM_UEL_TET""},
  ""NL"" :  { ""name"" : ""Netherlands"",""ports"":""AMS_MST_EIN_GRQ_RTM""},
  ""NO"" :  { ""name"" : ""Norway"",""ports"":""AES_ANX_ALF_BNN_BOO_BGO_BJF_KRS_BDU_EVE_FRO_OSL_HAU_HAA_KSU_KKN_MOL_MJF_LKL_RRS_SSJ_TOS_TRF_TRD_SVG_HFT_HVG_MEH_VDS_HOV_BVG_FDE_LKN_OSY_MQN_RVK_RET_SDN_SOG_SVJ_SOJ_VAW""},
  ""NP"" :  { ""name"" : ""Nepal"",""ports"":""KTM""},
  ""NZ"" :  { ""name"" : ""New Zealand"",""ports"":""AKL_CHC_ZQN_WLG""},
  ""OM"" :  { ""name"" : ""Oman"",""ports"":""MCT_SLL_OHS""},
  ""PG"" :  { ""name"" : ""Papua New Guinea"",""ports"":""POM""},
  ""PK"" :  { ""name"" : ""Pakistan"",""ports"":""LYP_KHI_LHE_MUX_PEW_UET_ISB_SKT""},
  ""PL"" :  { ""name"" : ""Poland"",""ports"":""SZY_GDN_KRK_KTW_POZ_RZE_SZZ_WAW_WRO_BZG_LCJ_WMI_LUZ""},
  ""PT"" :  { ""name"" : ""Portugal"",""ports"":""FAO_TER_PDL_OPO_PXO_LIS_FNC""},
  ""QA"" :  { ""name"" : ""Qatar"",""ports"":""DOH""},
  ""RO"" :  { ""name"" : ""Romania"",""ports"":""BCM_CND_CLJ_CRA_IAS_OTP_SBZ_SUJ_SCV_TGM_TSR""},
  ""RS"" :  { ""name"" : ""Serbia"",""ports"":""BEG_INI""},
  ""RU"" :  { ""name"" : ""Russia"",""ports"":""UUD_LED_MMK_KGD_KEJ_OMS_KRR_MCX_MRV_ROV_AER_ASF_VOG_CEK_MQF_PEE_SGC_SVX_SVO_VOZ_VKO_SCW_KZN_REN_UFA_DME_OVB_TJM_KUF_TOF_AAQ_ARH_KJA_PES_GRV_OGZ_KVX_CSY_NBC_ULV_GDZ_IGT""},
  ""SA"" :  { ""name"" : ""Saudi Arabia"",""ports"":""AHB_DMM_GIZ_ELQ_HAS_JED_MED_RUH_SHW_TUU_TIF_YNB_AJF_WAE""},
  ""SC"" :  { ""name"" : ""Seychelles"",""ports"":""SEZ""},
  ""SD"" :  { ""name"" : ""Sudan"",""ports"":""KRT""},
  ""SE"" :  { ""name"" : ""Sweden"",""ports"":""GOT_NYO_MMX_VXO_KRN_UME_VST_LLA_ARN_VBY_OSD_KSD""},
  ""SG"" :  { ""name"" : ""Singapore"",""ports"":""SIN""},
  ""SI"" :  { ""name"" : ""Slovenia"",""ports"":""LJU""},
  ""SJ"" :  { ""name"" : ""Svalbard and Jan Mayen"",""ports"":""LYR""},
  ""SK"" :  { ""name"" : ""Slovakia"",""ports"":""BTS_KSC_TAT""},
  ""SN"" :  { ""name"" : ""Senegal"",""ports"":""DSS""},
  ""SO"" :  { ""name"" : ""Somalia"",""ports"":""HGA""},
  ""TH"" :  { ""name"" : ""Thailand"",""ports"":""KBV_HKT_BKK""},
  ""TN"" :  { ""name"" : ""Tunisia"",""ports"":""MIR_TUN_DJE_NBE""},
  ""TO"" :  { ""name"" : ""Tonga"",""ports"":""TBU""},
  ""TR"" :  { ""name"" : ""Turkey"",""ports"":""ESB_ADA_AYT_GZT_KYA_MZH_VAS_MLX_ASR_DNZ_IST_ADB_DLM_EZS_DIY_ERC_ERZ_TZX_VAN_BAL_BJV_SAW_KSY_KCM_AJI_EDO_SZF_MQM_AOE_MSR_NOP_TEQ_HTY_GZP_KZR_GNY_OGU_BGG""},
  ""TZ"" :  { ""name"" : ""Tanzania"",""ports"":""ZNZ""},
  ""UA"" :  { ""name"" : ""Ukraine"",""ports"":""KBP_SIP_IEV_LWO_ODS_OZH_HRK""},
  ""US"" :  { ""name"" : ""United States"",""ports"":""BOS_OAK_SFO_LAX_EWR_FLL_MIA_SEA_PVD_SWF_IAD_HNL_DEN_RSW_JFK_BDL_ORD_MSP_LAS_MCO""},
  ""XK"" :  { ""name"" : ""Kosovo"",""ports"":""PRN""},
  ""ZA"" :  { ""name"" : ""South Africa"",""ports"":""CPT_JNB""},
  ""ZW"" :  { ""name"" : ""Zimbabwe"",""ports"":""BUQ_VFA_HRE""},
  ""XXX"" : { ""name"" : ""Anywhere"",""ports"":""XXX""}
}
";

        #endregion
    }
}