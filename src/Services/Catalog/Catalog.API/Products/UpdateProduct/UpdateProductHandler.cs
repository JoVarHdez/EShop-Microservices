using Catalog.API.Models;
using FluentValidation;
using Marten;

namespace Catalog.API.Products.UpdateProduct
{
    public record UpdateProductCommand(Guid Id, string Name, List<string> Categories, string Description, string ImageUrl, decimal Price);

    public abstract record UpdateProductCommandResult;
    public record UpdateProductResult(bool IsSuccess) : UpdateProductCommandResult;
    public record UpdateProductNotFound : UpdateProductCommandResult;

    public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Categories).NotEmpty().WithMessage("At least one category is required.");
            RuleFor(x => x.Description).NotEmpty().MaximumLength(500).WithMessage("Description is required and must be less than 500 characters.");
            RuleFor(x => x.ImageUrl).NotEmpty().Must(uri => Uri.IsWellFormedUriString(uri, UriKind.Absolute)).WithMessage("ImageUrl must be a valid URL.");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0.");
        }
    }

    public class UpdateProductCommandHandler(IDocumentSession session)
    {
        public async Task<UpdateProductCommandResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await session.LoadAsync<Product>(request.Id, cancellationToken);

            if (product is null)
                return new UpdateProductNotFound();

            product.Name = request.Name;
            product.Description = request.Description;
            product.Categories = request.Categories;
            product.ImageUrl = request.ImageUrl;
            product.Price = request.Price;

            session.Update(product);
            await session.SaveChangesAsync(cancellationToken);

            return new UpdateProductResult(true);
        }
    }
}
