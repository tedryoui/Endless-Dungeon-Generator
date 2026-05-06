namespace Extensions
{
    public static class FloatExtension
    {
        public static float[][] Transpose(this float[][] matrix)
        {
            int rows = matrix.Length;
            int cols = matrix[0].Length;

            float[][] result = new float[cols][];
            for (int i = 0; i < cols; i++)
            {
                result[i] = new float[rows];
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    result[c][r] = matrix[r][c];
                }
            }

            return result;
        }
    }
}