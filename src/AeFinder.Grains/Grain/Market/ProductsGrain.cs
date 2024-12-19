using AeFinder.Grains.State.Market;
using AeFinder.Market;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Market;

public class ProductsGrain : AeFinderGrain<List<ProductState>>, IProductsGrain
{
    private readonly IObjectMapper _objectMapper;

    public ProductsGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task InitializeProductsInfoAsync(ProductDto dto)
    {
        await ReadStateAsync();

        var productInfo = _objectMapper.Map<ProductDto, ProductState>(dto);
        if (productInfo.ProductId.IsNullOrEmpty())
        {
            throw new Exception("Product id can not be null");
        }

        if (this.State == null || this.State.Count == 0)
        {
            State = new List<ProductState>();
            State.Add(productInfo);
            await WriteStateAsync();
            return;
        }

        //Products with the same type and specifications can be considered identical.
        var product = State.FirstOrDefault(p =>
            p.ProductType == dto.ProductType && p.ProductSpecifications == dto.ProductSpecifications &&
            p.IsActive == true);

        if (product == null)
        {
            State.Add(productInfo);
            await WriteStateAsync();
            return;
        }

        product.ProductName = dto.ProductName;
        product.Description = dto.Description;
        product.MonthlyUnitPrice = dto.MonthlyUnitPrice;
        
        await WriteStateAsync();
    }

    public async Task<ProductDto> GetProductInfoByIdAsync(string productId)
    {
        var product = State.FirstOrDefault(p => p.ProductId == productId);
        return _objectMapper.Map<ProductState, ProductDto>(product);
    }

    public async Task<ProductDto> GetFreeApiQueryCountProductAsync()
    {
        var productState = State.FirstOrDefault(p =>
            p.ProductType == ProductType.ApiQueryCount && p.IsActive == true && p.MonthlyUnitPrice == 0);
        // return Convert.ToInt32(productState.ProductSpecifications);
        return _objectMapper.Map<ProductState, ProductDto>(productState);
    }

    public async Task<ProductDto> GetRegularApiQueryCountProductAsync()
    {
        var productState = State.FirstOrDefault(p =>
            p.ProductType == ProductType.ApiQueryCount && p.IsActive == true && p.MonthlyUnitPrice > 0);
        return _objectMapper.Map<ProductState, ProductDto>(productState);
    }

    public async Task<List<ProductDto>> GetFullPodResourceProductsAsync()
    {
        var products = State.Where(p => p.ProductType == ProductType.FullPodResource && p.IsActive == true).ToList();
        return _objectMapper.Map<List<ProductState>, List<ProductDto>>(products);
    }
}