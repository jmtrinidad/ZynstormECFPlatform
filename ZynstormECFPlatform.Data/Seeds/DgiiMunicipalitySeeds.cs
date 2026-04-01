using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Seeds;

public static class DgiiMunicipalitySeeds
{
    public static void Seed(EntityTypeBuilder<DgiiMunicipality> builder)
    {
        builder.HasData(
            // 01 - DISTRITO NACIONAL
            new DgiiMunicipality { DgiiMunicipalityId = 1, Code = "010000", Name = "DISTRITO NACIONAL", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 2, Code = "010100", Name = "MUNICIPIO SANTO DOMINGO DE GUZMÁN", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 3, Code = "010101", Name = "SANTO DOMINGO DE GUZMÁN (D. M.).", IsProvince = false },

            // 02 - AZUA
            new DgiiMunicipality { DgiiMunicipalityId = 10, Code = "020000", Name = "PROVINCIA AZUA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 11, Code = "020100", Name = "MUNICIPIO AZUA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 12, Code = "020101", Name = "AZUA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 13, Code = "020102", Name = "BARRO ARRIBA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 14, Code = "020103", Name = "LAS BARÍAS-LA ESTANCIA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 15, Code = "020104", Name = "LOS JOVILLOS (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 16, Code = "020105", Name = "PUERTO VIEJO (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 17, Code = "020106", Name = "BARRERAS (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 18, Code = "020107", Name = "DOÑA EMMA BALAGUER VIUDA VALLEJO (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 19, Code = "020108", Name = "CLAVELLINA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 20, Code = "020109", Name = "LAS LOMAS (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 21, Code = "020200", Name = "MUNICIPIO LAS CHARCAS", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 22, Code = "020201", Name = "LAS CHARCAS (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 23, Code = "020202", Name = "PALMAR DE OCOA (D. M.).", IsProvince = false },
            
            // 03 - BAHORUCO
            new DgiiMunicipality { DgiiMunicipalityId = 50, Code = "030000", Name = "PROVINCIA BAHORUCO", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 51, Code = "030001", Name = "MUNICIPIO NEIBA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 52, Code = "030101", Name = "NEIBA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 53, Code = "030102", Name = "EL PALMAR  (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 54, Code = "030200", Name = "MUNICIPIO GALVÁN", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 55, Code = "030201", Name = "GALVÁN (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 56, Code = "030202", Name = "EL SALADO (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 57, Code = "030300", Name = "MUNICIPIO TAMAYO", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 58, Code = "030301", Name = "TAMAYO (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 59, Code = "030302", Name = "UVILLA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 60, Code = "030303", Name = "SANTANA (D. M.).", IsProvince = false },
            
            // 04 - BARAHONA
            new DgiiMunicipality { DgiiMunicipalityId = 80, Code = "040000", Name = "PROVINCIA BARAHONA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 81, Code = "040100", Name = "MUNICIPIO BARAHONA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 82, Code = "040101", Name = "BARAHONA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 83, Code = "040102", Name = "EL CACHÓN (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 84, Code = "040103", Name = "LA GUÁZARA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 85, Code = "040104", Name = "VILLA CENTRAL (D. M.).", IsProvince = false },

            // 05 - DAJABÓN
            new DgiiMunicipality { DgiiMunicipalityId = 110, Code = "050000", Name = "PROVINCIA DAJABÓN", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 111, Code = "050100", Name = "MUNICIPIO DAJABÓN", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 112, Code = "050101", Name = "DAJABÓN (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 113, Code = "050102", Name = "CAÑONGO (D. M.).", IsProvince = false },

            // 06 - DUARTE
            new DgiiMunicipality { DgiiMunicipalityId = 130, Code = "060000", Name = "PROVINCIA DUARTE", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 131, Code = "060100", Name = "MUNICIPIO SAN FRANCISCO DE MACORÍS", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 132, Code = "060101", Name = "SAN FRANCISCO DE MACORÍS (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 133, Code = "060102", Name = "LA PEÑA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 134, Code = "060103", Name = "CENOVÍ (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 135, Code = "060104", Name = "JAYA (D. M.).", IsProvince = false },

            // 07 - ELÍAS PIÑA
            new DgiiMunicipality { DgiiMunicipalityId = 150, Code = "070000", Name = "PROVINCIA ELÍAS PIÑA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 151, Code = "070100", Name = "MUNICIPIO COMENDADOR", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 152, Code = "070101", Name = "COMENDADOR (D. M.).", IsProvince = false },

            // 08 - EL SEIBO
            new DgiiMunicipality { DgiiMunicipalityId = 170, Code = "080000", Name = "PROVINCIA EL SEIBO", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 171, Code = "080100", Name = "MUNICIPIO EL SEIBO", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 172, Code = "080101", Name = "EL SEIBO (D. M.).", IsProvince = false },

            // 09 - ESPAILLAT
            new DgiiMunicipality { DgiiMunicipalityId = 190, Code = "090000", Name = "PROVINCIA ESPAILLAT", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 191, Code = "090100", Name = "MUNICIPIO MOCA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 192, Code = "090101", Name = "MOCA (D. M.).", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 193, Code = "090200", Name = "MUNICIPIO CAYETANO GERMOSÉN", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 194, Code = "090201", Name = "CAYETANO GERMOSÉN (D. M.).", IsProvince = false },

            // 10 - INDEPENDENCIA
            new DgiiMunicipality { DgiiMunicipalityId = 210, Code = "100000", Name = "PROVINCIA INDEPENDENCIA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 211, Code = "100100", Name = "MUNICIPIO JIMANÍ", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 212, Code = "100101", Name = "JIMANÍ (D. M.).", IsProvince = false },

            // 11 - LA ALTAGRACIA
            new DgiiMunicipality { DgiiMunicipalityId = 230, Code = "110000", Name = "PROVINCIA LA ALTAGRACIA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 231, Code = "110100", Name = "MUNICIPIO HIGÜEY", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 232, Code = "110101", Name = "HIGÜEY (D. M.).", IsProvince = false },

            // 12 - LA ROMANA
            new DgiiMunicipality { DgiiMunicipalityId = 250, Code = "120000", Name = "PROVINCIA LA ROMANA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 251, Code = "120100", Name = "MUNICIPIO LA ROMANA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 252, Code = "120101", Name = "LA ROMANA (D. M.).", IsProvince = false },

            // 13 - LA VEGA
            new DgiiMunicipality { DgiiMunicipalityId = 270, Code = "130000", Name = "PROVINCIA LA VEGA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 271, Code = "130100", Name = "MUNICIPIO LA VEGA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 272, Code = "130101", Name = "LA VEGA (D. M.).", IsProvince = false },

            // 14 - MARÍA TRINIDAD SÁNCHEZ
            new DgiiMunicipality { DgiiMunicipalityId = 290, Code = "140000", Name = "PROVINCIA MARÍA TRINIDAD SÁNCHEZ", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 291, Code = "140100", Name = "MUNICIPIO NAGUA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 292, Code = "140101", Name = "NAGUA (D. M.).", IsProvince = false },

            // 15 - MONTE CRISTI
            new DgiiMunicipality { DgiiMunicipalityId = 310, Code = "150000", Name = "PROVINCIA MONTE CRISTI", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 311, Code = "150100", Name = "MUNICIPIO MONTE CRISTI", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 312, Code = "150101", Name = "MONTE CRISTI (D. M.).", IsProvince = false },

            // 16 - PEDERNALES
            new DgiiMunicipality { DgiiMunicipalityId = 330, Code = "160000", Name = "PROVINCIA PEDERNALES", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 331, Code = "160100", Name = "MUNICIPIO PEDERNALES", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 332, Code = "160101", Name = "PEDERNALES", IsProvince = false },

            // 17 - PERAVIA
            new DgiiMunicipality { DgiiMunicipalityId = 350, Code = "170000", Name = "PROVINCIA PERAVIA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 351, Code = "170100", Name = "MUNICIPIO BANÍ", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 352, Code = "170101", Name = "BANÍ (D. M.).", IsProvince = false },

            // 18 - PUERTO PLATA
            new DgiiMunicipality { DgiiMunicipalityId = 370, Code = "180000", Name = "PROVINCIA PUERTO PLATA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 371, Code = "180100", Name = "MUNICIPIO PUERTO PLATA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 372, Code = "180101", Name = "PUERTO PLATA (D. M.).", IsProvince = false },

            // 19 - HERMANAS MIRABAL
            new DgiiMunicipality { DgiiMunicipalityId = 390, Code = "190000", Name = "PROVINCIA HERMANAS MIRABAL", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 391, Code = "190100", Name = "MUNICIPIO SALCEDO", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 392, Code = "190101", Name = "SALCEDO", IsProvince = false },

            // 20 - SAMANÁ
            new DgiiMunicipality { DgiiMunicipalityId = 410, Code = "200000", Name = "PROVINCIA SAMANÁ", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 411, Code = "200100", Name = "MUNICIPIO SAMANÁ", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 412, Code = "200101", Name = "SAMANÁ", IsProvince = false },

            // 21 - SAN CRISTÓBAL
            new DgiiMunicipality { DgiiMunicipalityId = 430, Code = "210000", Name = "PROVINCIA SAN CRISTÓBAL", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 431, Code = "210100", Name = "MUNICIPIO SAN CRISTÓBAL", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 432, Code = "210101", Name = "SAN CRISTÓBAL (D. M.).", IsProvince = false },

            // 22 - SAN JUAN
            new DgiiMunicipality { DgiiMunicipalityId = 450, Code = "220000", Name = "PROVINCIA SAN JUAN", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 451, Code = "220100", Name = "MUNICIPIO SAN JUAN", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 452, Code = "220101", Name = "SAN JUAN", IsProvince = false },

            // 23 - SAN PEDRO DE MACORÍS
            new DgiiMunicipality { DgiiMunicipalityId = 470, Code = "230000", Name = "PROVINCIA SAN PEDRO DE MACORÍS", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 471, Code = "230100", Name = "MUNICIPIO SAN PEDRO DE MACORÍS", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 472, Code = "230101", Name = "SAN PEDRO DE MACORÍS", IsProvince = false },

            // 24 - SÁNCHEZ RAMÍREZ
            new DgiiMunicipality { DgiiMunicipalityId = 490, Code = "240000", Name = "PROVINCIA SANCHEZ RAMÍREZ", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 491, Code = "240100", Name = "MUNICIPIO COTUÍ", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 492, Code = "240101", Name = "COTUÍ", IsProvince = false },

            // 25 - SANTIAGO
            new DgiiMunicipality { DgiiMunicipalityId = 510, Code = "250000", Name = "PROVINCIA SANTIAGO", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 511, Code = "250100", Name = "MUNICIPIO SANTIAGO", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 512, Code = "250101", Name = "SANTIAGO", IsProvince = false },

            // 26 - SANTIAGO RODRÍGUEZ
            new DgiiMunicipality { DgiiMunicipalityId = 530, Code = "260000", Name = "PROVINCIA SANTIAGO RODRÍGUEZ", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 531, Code = "260100", Name = "MUNICIPIO SAN IGNACIO DE SABANETA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 532, Code = "260101", Name = "SAN IGNACIO DE SABANETA (D. M.).", IsProvince = false },

            // 27 - VALVERDE
            new DgiiMunicipality { DgiiMunicipalityId = 550, Code = "270000", Name = "PROVINCIA VALVERDE", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 551, Code = "270100", Name = "MUNICIPIO MAO", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 552, Code = "270101", Name = "MAO (D. M.).", IsProvince = false },

            // 28 - MONSEÑOR NOUEL
            new DgiiMunicipality { DgiiMunicipalityId = 570, Code = "280000", Name = "PROVINCIA MONSEÑOR NOUEL", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 571, Code = "280100", Name = "MUNICIPIO BONAO", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 572, Code = "280101", Name = "BONAO (D. M.).", IsProvince = false },

            // 29 - MONTE PLATA
            new DgiiMunicipality { DgiiMunicipalityId = 590, Code = "290000", Name = "PROVINCIA MONTE PLATA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 591, Code = "290100", Name = "MUNICIPIO MONTE PLATA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 592, Code = "290101", Name = "MONTE PLATA (D. M.).", IsProvince = false },

            // 30 - HATO MAYOR
            new DgiiMunicipality { DgiiMunicipalityId = 610, Code = "300000", Name = "PROVINCIA HATO MAYOR", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 611, Code = "300100", Name = "MUNICIPIO HATO MAYOR", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 612, Code = "300101", Name = "HATO MAYOR (D. M.).", IsProvince = false },

            // 31 - SAN JOSÉ DE OCOA
            new DgiiMunicipality { DgiiMunicipalityId = 630, Code = "310000", Name = "PROVINCIA SAN JOSÉ DE OCOA", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 631, Code = "310100", Name = "MUNICIPIO SAN JOSÉ DE OCOA", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 632, Code = "310101", Name = "SAN JOSÉ DE OCOA (D. M.).", IsProvince = false },

            // 32 - SANTO DOMINGO
            new DgiiMunicipality { DgiiMunicipalityId = 650, Code = "320000", Name = "PROVINCIA SANTO DOMINGO", IsProvince = true },
            new DgiiMunicipality { DgiiMunicipalityId = 651, Code = "320100", Name = "MUNICIPIO SANTO DOMINGO ESTE", IsProvince = false },
            new DgiiMunicipality { DgiiMunicipalityId = 652, Code = "320101", Name = "SANTO DOMINGO ESTE (D. M.).", IsProvince = false }
        );
    }
}
