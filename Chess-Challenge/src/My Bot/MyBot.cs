using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ChessChallenge.API;
// Revenge bot
public struct EvaluatedMove
{   
    public EvaluatedMove(Int32 evalue, Move move)
    {
        Move = move;
        Evaluation = evalue;
    }

    public Move Move { get; }
    public Int32 Evaluation { get; }

    public override string ToString()
    {
        return String.Format("[{0}] {1}", Move.ToString(), Evaluation.ToString());
    }
}  
public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    // If there isn't a revenge target we play as optimally as possible - otherwise we are BLINDED BY REVENGE!
    Piece? revengeTarget = null;
    int maxSearchBudget = 2; // in ply
    Random rnd = new Random();

    private int evaluateMove(Board board, Move move, bool white, int searchBudget)
    {
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        Piece pieceUsed = board.GetPiece(move.StartSquare);

        bool risky = board.SquareIsAttackedByOpponent(move.TargetSquare);

        //Console.WriteLine("Searching with depth {0}", Math.Abs(maxSearchBudget-searchBudget)+1);

        board.MakeMove(move);

    
        // Evaluate this board state with a piece of the evaluation budget
        int eval = 0;

        // Score for being able to capture a piece
        eval += pieceValues[(int)capturedPiece.PieceType];

        // Score for mate in one only, but we can do up to three through recursion. 
        eval += board.IsInCheckmate() ? pieceValues[(int)PieceType.King] : 0;

        // Score progressed pawns
        eval += pieceUsed.IsPawn ? 10 : 0;

        // Center control bonus 
        if (move.TargetSquare.File >= 'C' && move.TargetSquare.File <= 'F' && move.TargetSquare.Rank >= 3 && move.TargetSquare.Rank <= 6)
        {
            eval += 10;
        }

        // Negatively score moving somewhere the opponent can attack with the assumption the piece dies. 
        eval -= risky ? Convert.ToInt32(pieceValues[(int)pieceUsed.PieceType] * 0.5) : 0; 

        // Add the evaluation points of moves that can branch from this one (i.e threatening other pieces by being able to capture them a move or two ahead)
        // Maybe we could cull some time by not searching from already bad moves

        if (searchBudget > 0)
        {
            foreach (Move depth_move in board.GetLegalMoves())
            {
                eval += evaluateMove(board, depth_move, !white, searchBudget - 1);
            }
        }

        board.UndoMove(move);

        return eval + (searchBudget == maxSearchBudget ? rnd.Next(-20, 20) : 0);
    }

    private Int32 getValue(Board board, bool white) 
    {
        Int32 value = 0;

        foreach (PieceList pieceList in board.GetAllPieceLists()) 
        {
               foreach (Piece piece in pieceList)
            {   
                if (piece.IsWhite == white)
                {
                    value += pieceValues[(int)piece.PieceType];
                }
            }
        }
        return value;
    }
    public Move Think(Board board, Timer timer)
    {

  
        Move[] moves = board.GetLegalMoves();
        List<EvaluatedMove>? evaluations = Array.Empty<EvaluatedMove>().ToList();

        foreach (Move move in moves) {
            int evaluation = evaluateMove(board, move, board.IsWhiteToMove, maxSearchBudget);

            evaluations.Add(
                new EvaluatedMove(
                    evaluation,
                    move
                )
           );
        }

        evaluations = evaluations.OrderByDescending(
            eval => eval.Evaluation
        ).ToList();


        Console.WriteLine("Ply: {0}", board.PlyCount);
        Console.WriteLine("Evaluations: {");
       // if (board.PlyCount == 1)
       // {
            int idx = 0;
            foreach (EvaluatedMove e in evaluations)
            {
             
                Console.WriteLine("{0} - {1}", idx, e);
                idx++;
              
            }
       // }
        Console.WriteLine("}");
        Move final = evaluations[0].Move;

        return final;
    }
}