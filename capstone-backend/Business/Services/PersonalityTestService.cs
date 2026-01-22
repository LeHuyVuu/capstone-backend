using capstone_backend.Business.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace capstone_backend.Business.Services
{
    public class PersonalityTestService : IPersonalityTestService
    {
		private readonly IUnitOfWork _unitOfWork;

        public PersonalityTestService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        
    }
}
