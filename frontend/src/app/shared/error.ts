export function handleError(error: any) {
  // if (error.status == 400) {
  //   return error.error;
  // } else {
  //   return "There was an error during processing your request. Try again later.";
  // }

  //TEMPORARY
  return error.error;
}
