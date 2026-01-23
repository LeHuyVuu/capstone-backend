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
                testType.Code = GenerateTestTypeCode(request.TotalQuestions);

                // Creating
                await _unitOfWork.TestTypes.AddAsync(testType);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> DeleteTestTypeAsync(int id)
        {
            try
            {
                var exist = await _unitOfWork.TestTypes.GetByIdAsync(id);
                if (exist == null)
                    throw new Exception("Test type not found");

                exist.IsDeleted = true;
                exist.IsActive = false;

                _unitOfWork.TestTypes.Update(exist);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<TestTypeResponse>> GetAllTestTypeAsync(string role = "MEMBER")
        {
            try
            {
                var testTypes = new List<TestType>();
                if (role.ToUpper() == "ADMIN")
                {
                    testTypes = (await _unitOfWork.TestTypes.GetAllAsync()).ToList();
                }
                else
                {
                    testTypes = (await _unitOfWork.TestTypes.GetAllTestTypeMemberAsync()).ToList();
                }

                var response = new List<TestTypeResponse>();

                // Mapping
                foreach (var testType in testTypes)
                {
                    response.Add(_mapper.Map<TestTypeResponse>(testType));
                }

                return response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<TestTypeDetailDto?> GetByIdAsync(int id, string role = "MEMBER")
        {
            try
            {
                var isAdmin = role.ToUpper() == "ADMIN";

                var testType = role.ToUpper() == "ADMIN"
                    ? await _unitOfWork.TestTypes.GetByIdAsync(id)
                    : await _unitOfWork.TestTypes.GetByIdForUserAsync(id);

                if (testType == null)
                    throw new Exception("Test type not found");

                var versions = await _unitOfWork.Questions.GetAllVersionsAsync(id);

                if (!isAdmin)
                {
                    versions = versions
                        .Where(v => v.IsActive)
                        .ToList();
                }

                var response = _mapper.Map<TestTypeDetailDto>(testType);
                response.Versions = versions;
                response.LastestVersion = versions
                        .FirstOrDefault(v => v.IsActive)
                        ?.Version ?? 0;

                return response;
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
                exist.Code = GenerateTestTypeCode(exist.TotalQuestions.Value);

                _unitOfWork.TestTypes.Update(exist);
                return await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string GenerateTestTypeCode(int totalQuestions, string type = "MBTI")
        {
            var utc = DateTime.UtcNow;
            var unix = new DateTimeOffset(utc).ToUnixTimeSeconds();

            return $"TT_{type.ToUpper()}_{totalQuestions}_{unix}";
        }
    }
}
