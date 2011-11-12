using System;
using System.Collections.Generic;
using System.Linq;
using FubuMVC.Core.Continuations;

namespace AddValidationBehaviour
{
    public class ProductController
    {
        private readonly IProductRepository _repository;

        public ProductController(IProductRepository repository)
        {
            _repository = repository;
        }

        public ProductList get_products(ProductListInputModel input)
        {
            return _repository.GetProducts();
        }

        public EditProductViewModel get_product_edit_Id(EditProductInputModel input)
        {
            var product = _repository.GetProducts().Where(x => x.Id == input.Id).First();

            return new EditProductViewModel
                       {
                           Id = product.Id,
                           Name = product.Name,
                           Price = product.Price.ToString()
                       };
        }

        public FubuContinuation post_product_edit_Id(SaveProductInputModel input)
        {
            var product = _repository.GetProducts().Where(x => x.Id == input.Id).First();

            try
            {
                product.Name = input.Name;
                product.Price = decimal.Parse(input.Price);
                return FubuContinuation.RedirectTo(new ProductListInputModel());
            }
            catch
            {
                return FubuContinuation.TransferTo(new ReEditProductInputModel
                                                       {
                                                           Id = input.Id,
                                                           Name = input.Name,
                                                           Price = input.Price
                                                       });
            }
        }

        public EditProductViewModel post_product_reedit_Id(ReEditProductInputModel input)
        {
            return new EditProductViewModel
            {
                Id = input.Id,
                Name = input.Name,
                Price = input.Price,
                Message = "Save Failed"
            };
        }
    }

    public class ReEditProductInputModel : SaveProductInputModel
    {
    }

    public class ProductListInputModel
    {
    }

    public class SaveProductInputModel
    {
        public SaveProductInputModel()
        {
        }

        public SaveProductInputModel(int id)
        {
            Id = id;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Price { get; set; }
    }

    public class EditProductInputModel
    {
        public EditProductInputModel()
        {
            
        }
        public EditProductInputModel(int id)
        {
            Id = id;
        }

        public int Id { get; set; }
    }

    public interface IProductRepository
    {
        ProductList GetProducts();
    }

    public class ProductRepository : IProductRepository
    {
        private readonly ProductList _productList = new ProductList
                                                        {
                                                            new Product {Id = 1, Name = "Peas", Price = 20},
                                                            new Product {Id = 2, Name = "Carrots", Price = 30 }
                                                        };

        public ProductList GetProducts()
        {
            return _productList;
        }
    }

    public class EditProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Price { get; set; }

        public string Message { get; set; }
    }

    public class ProductList : List<Product>{}
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}