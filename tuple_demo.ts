let person: [string, number] = ["Alice", 30];
let coordinates: [number, number] = [10, 20];
function displayTuple(tuple: [string, number]) {
    console.log(`Name: ${tuple[0]}, Age: ${tuple[1]}`);
}
displayTuple(person); // Output: Name: Alice, Age: 30
console.log(`Coordinates: (${coordinates[0]}, ${coordinates[1]})`); 