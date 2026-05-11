using FluentValidation;
using turf_management_system.DTOs.Turf;

namespace turf_management_system.Validators
{
    public class CreateTurfDtoValidator : AbstractValidator<CreateTurfDto>
    {
        public CreateTurfDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Location).NotEmpty().MaximumLength(300);
            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PricePerHour).GreaterThan(0);
            RuleFor(x => x.SportType).NotEmpty().MaximumLength(100);
        }
    }

    public class UpdateTurfDtoValidator : AbstractValidator<UpdateTurfDto>
    {
        public UpdateTurfDtoValidator()
        {
            RuleFor(x => x.Name).MaximumLength(150).When(x => x.Name != null);
            RuleFor(x => x.Location).MaximumLength(300).When(x => x.Location != null);
            RuleFor(x => x.City).MaximumLength(100).When(x => x.City != null);
            RuleFor(x => x.PricePerHour).GreaterThan(0).When(x => x.PricePerHour.HasValue);
            RuleFor(x => x.SportType).MaximumLength(100).When(x => x.SportType != null);
        }
    }

    public class CreateTurfSlotDtoValidator : AbstractValidator<CreateTurfSlotDto>
    {
        public CreateTurfSlotDtoValidator()
        {
            RuleFor(x => x.StartTime).NotEmpty();
            RuleFor(x => x.EndTime).NotEmpty().GreaterThan(x => x.StartTime);
            RuleFor(x => x.DayOfWeek).InclusiveBetween(0, 6).When(x => x.DayOfWeek.HasValue);
        }
    }
}
