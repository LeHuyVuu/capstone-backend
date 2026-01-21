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

        public async Task<TestTypeResponse?> GetByIdAsync(int id, string role = "MEMBER")
        {
            try
            {
                var response = new TestTypeResponse();

                if (role.ToUpper() == "ADMIN")
                {
                    response = _mapper.Map<TestTypeResponse>(await _unitOfWork.TestTypes.GetByIdAsync(id));
                }
                else
                {
                    response = _mapper.Map<TestTypeResponse>(await _unitOfWork.TestTypes.GetByIdForUserAsync(id));
                }

                if (response == null)
                {
                    throw new Exception("Test type not found");
                }

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
