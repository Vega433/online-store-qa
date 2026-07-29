using Microsoft.AspNetCore.Mvc;
using OnlineStore.Application.DTOs.Carts;
using OnlineStore.Application.Interfaces;

namespace OnlineStore.Api.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController(ICartService cartService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CartDto>> Create(CancellationToken cancellationToken)
    {
        var cart = await cartService.CreateAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { cartId = cart.Id }, cart);
    }

    [HttpGet("{cartId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> GetById(Guid cartId, CancellationToken cancellationToken)
    {
        return Ok(await cartService.GetByIdAsync(cartId, cancellationToken));
    }

    [HttpDelete("{cartId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid cartId, CancellationToken cancellationToken)
    {
        await cartService.DeleteAsync(cartId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Add one or more products. Body: { "items": [ { "productId": "...", "quantity": 1 }, ... ] }
    /// </summary>
    [HttpPost("{cartId:guid}/items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> AddItems(
        Guid cartId,
        [FromBody] AddCartItemsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await cartService.AddItemsAsync(cartId, request, cancellationToken));
    }

    [HttpPut("{cartId:guid}/items/{productId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateItem(
        Guid cartId,
        Guid productId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await cartService.UpdateItemAsync(cartId, productId, request, cancellationToken));
    }

    [HttpDelete("{cartId:guid}/items/{productId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveItem(
        Guid cartId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return Ok(await cartService.RemoveItemAsync(cartId, productId, cancellationToken));
    }
}
