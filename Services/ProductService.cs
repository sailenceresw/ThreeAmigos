using ecommerce.Models;
using ecommerce.Repository;
using ecommerce.ViewModels.Product;

namespace ecommerce.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository productrepository;
        private readonly ICategoryRepository categoryRepository;
        private readonly Context _context;

        public ProductService(IProductRepository _repository, ICategoryRepository categoryRepository, Context context)
        {
            productrepository = _repository;
            this.categoryRepository = categoryRepository;
            this._context = context;
        }

        public List<Product> GetAll(string include = null)
        {
            if (include == null)
            {
                return productrepository.GetAll();
            }
            return productrepository.GetAll(include);
        }

        public List<Product> GetPageList(int skipstep, int pageSize)
        {
            return productrepository.GetPageList(skipstep, pageSize);
        }

        public Product Get(int Id)
        {
            return productrepository.Get(Id);
        }

        public List<Product> Get(Func<Product, bool> where)
        {
            return productrepository.Get(where);
        }

		public void Insert(Product product)
        {
            productrepository.Insert(product);
        }

        public void Update(Product product)
        {
            productrepository.Update(product);
        }

        public void Delete(Product product)
        {
            productrepository.Delete(product);
        }

        public void Save()
        {
            productrepository.Save();
        }

        public async Task<ProductWithCatNameAndComments> WithCatNameAndComments(int id)
        {
            Product p = productrepository.Get(id);
            ProductWithCatNameAndComments product = new()
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.ImageUrl,
                Color = p.Color,
                Description = p.Description,
                Price = p.Price,
                Quantity = p.Quantity,
                Rating = p.Rating
            };

            Category c = categoryRepository.Get(p.CategoryId);
            product.CateName = c.Name;
            return product;
        }

        public Product Insert(ProductWithListOfCatesViewModel p)
        {
            var existingProduct = _context.Product.FirstOrDefault(pr => pr.Name.Trim() == p.Name.Trim());

            Product product = new()
            {
                Id = p.Id,
                Name = p.Name,
                Color = p.Color,
                Description = p.Description,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                Quantity = p.Quantity,
                Rating = p.Rating,
                CategoryId = p.CategoryId,
                SupplierId = p.SupplierId,
                IsParent = existingProduct == null ? true : false,
                StockStatus = p.Quantity > 0 ? "IN STOCK" : "OUT OF STOCK"
            };

            productrepository.Insert(product);
            productrepository.Save();

            return product;
        }

        public ProductWithListOfCatesViewModel GetViewModel(int id)
        {
            Product p = productrepository.Get(id);
            ProductWithListOfCatesViewModel prd = new()
            {
                Id = p.Id,
                Name = p.Name,
                Color = p.Color,
                Description = p.Description,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                Quantity = p.Quantity,
                Rating = p.Rating,
                CategoryId = p.CategoryId,
                SupplierId = p.SupplierId
            };
            prd.categories = categoryRepository.GetAll();
            return prd;
        }

        public void Update(ProductWithListOfCatesViewModel p)
        {
            Product product = productrepository.Get(p.Id);
            product.Name = p.Name;
            product.Color = p.Color;
            product.Description = p.Description;
            product.ImageUrl = p.ImageUrl != "" ? p.ImageUrl : product.ImageUrl;
            product.Price = p.Price;
            product.Quantity = product.Quantity + p.Quantity;
            product.StockStatus = (product.Quantity + p.Quantity) > 0 ? "IN STOCK" : "OUT OF STOCK";
            product.Rating = p.Rating;
            product.CategoryId = p.CategoryId;
            product.SupplierId = p.SupplierId;
            productrepository.Update(product);
            productrepository.Save();
        }
    }
}
