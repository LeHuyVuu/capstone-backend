using AutoMapper;
using capstone_backend.Business.DTOs.TestType;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Services
{
    public class TestTypeService : ITestTypeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TestTypeService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<int> CreateTestTypeAsync(CreateTestTypeResquest request)
        {
            try
            {
                // Validation
                if (await _unitOfWork.TestTypes.GetByNameAsync(request.Name) != null)
                    throw new Exception("Test type with the same name already exists");

                // Mapping
                var testType = _mapper.Map<TestType>(request);

                // Creating
                await _unitOfWork.TestTypes.AddAsync(testType);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> UpdateTestTypeAsync(int id, UpdateTestTypeRequest request)
        {
            try
            {
                var exist = await _unitOfWork.TestTypes.GetByIdAsync(id);
                if (exist == null)
                    throw new Exception("Test type not found");

                // Mapping
                _mapper.Map(request, exist);
                _unitOfWork.TestTypes.Update(exist);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
