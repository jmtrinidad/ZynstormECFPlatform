namespace ZynstormECFPlatform.Dtos
{
    public class EcfDocumentViewDto
    {
        public string Id { get; set; } = null!; // GuidId
        public string Ncf { get; set; } = null!;
        public string ClientName { get; set; } = null!; // CustomerName
        public string ClientRnc { get; set; } = null!; // CustomerRnc
        public string Type { get; set; } = null!; // FE, FC, NC, ND
        public decimal Amount { get; set; } // Total
        public string Status { get; set; } = null!; // accepted, pending, rejected, processing
        public string? DgiiTrackId { get; set; } // TrackId de la última transmisión
        public string SentDate { get; set; } = null!; // Fecha de emisión formateada
        public string? ResponseDate { get; set; } // Fecha de procesamiento o respuesta
        public string? Xml { get; set; } // Solo poblado en consulta por GUID
    }

    public class EcfDocumentStatsDto
    {
        public int Aceptadas { get; set; }
        public int Pendientes { get; set; }
        public int Procesando { get; set; }
        public int Rechazadas { get; set; }
    }
}
