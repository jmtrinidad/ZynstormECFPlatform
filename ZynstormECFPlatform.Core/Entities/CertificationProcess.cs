using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Core.Entities
{
    public class CertificationProcess : BaseEntity
    {
        public int CertificationProcessId { get; set; }

        public int ClientId { get; set; }

        public DgiiEnvironment Environment { get; set; }

        public CertificationStatus Status { get; set; }

        public int? CurrentStepId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public virtual Client Client { get; set; } = null!;

        public virtual CertificationStep CertificationStep { get; set; } = null!;

        public virtual ICollection<CertificationDocument> CertificationDocuments { get; set; } = [];
    }
}