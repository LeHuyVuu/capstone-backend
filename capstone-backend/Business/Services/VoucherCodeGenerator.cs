using capstone_backend.Business.Interfaces;
using NanoidDotNet;

namespace capstone_backend.Business.Services
{
    public class VoucherCodeGenerator : IVoucherCodeGenerator
    {
        private readonly IUnitOfWork _unitOfWork;

        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int Size = 10;
        private const int MaxRetry = 20;

        public VoucherCodeGenerator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateUniqueCodeAsync()
        {
            for (int i = 0; i < MaxRetry; i++)
            {
                var code = Nanoid.Generate(Alphabet, Size);

                var existed = await _unitOfWork.VoucherItems.IsExistedCodeAsync(code);

                if (!existed)
                    return $"CP{code}";
                ;
            }

            return "";
        }
    }
}
