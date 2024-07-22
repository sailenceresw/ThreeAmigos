using ecommerce.Models;
using Microsoft.EntityFrameworkCore;
namespace ecommerce.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {

        public ProductRepository(Context _context) : base(_context)
        {
        }
        
        public Product Insert(Product product)
        {
            Context.Add(product);
            Context.SaveChanges();

            return product;
        }

        public void Update(Product product)
        {
            Context.Update(product);
        }

        public List<Product> GetAll(string? include = null)
        {
            if (include == null)
            {
                return Context.Product.Where(P => P.IsParent == true).ToList();
            }
            return Context.Product.Include(include).ToList();
        }

        public Product Get(int Id)
        {
            return Context.Product.FirstOrDefault(p=>p.Id==Id);
        }
        public List<Product> Get(Func<Product, bool> where)
        {
            return Context.Product.Where(where).ToList();
        }

        public void Delete(Product product)
        {
            Context.Remove(product);
        }

        public void Save()
        {
            Context.SaveChanges();
        }

        //-------------------------------------------------------------

        public List<Product> GetPageList(int skipstep, int pageSize)
        {
            return Context.Product.Where(P => P.IsParent == true).Skip(skipstep).Take(pageSize).ToList();
        }
    }
}
