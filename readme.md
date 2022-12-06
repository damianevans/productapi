# ASP.Net Product API
C# ASP.Net Core 6 Web API project

A simple asp.net web api that returns some basic product data.
Data is loaded from a JSON file (stored as an embedded resource in the project). There are 60K records in the file.

## Usage:
/products - returns all of the products in JSON format
/products/<product_id> - returns data for a given product id in JSON format. If a product doesn't exist then the API will return a HTTP 404
/products/allIds - return an array of all valid product Ids

## Note
Every 300th request to the  `/products/<product_id>` endpoint will generate a HTTP 500 error. This is intentional and consumers should make allowances for this in their code (e.g. retry pattern)
