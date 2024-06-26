﻿using Domain.Comment.Entity;

namespace Infra.Comment.Model;

public class CommentRetrieveResponse {
  public Guid PostId { get; set; }
  public Guid Id { get; set; }
  public string Value { get; set; }
  public List<CommentRetrieveResponse> SubComments { get; set; } = new();

  public static CommentRetrieveResponse From(CommentEntity entity) {
    return new CommentRetrieveResponse {
      PostId = entity.PostId,
      Id = entity.Id,
      Value = entity.Comment,
      SubComments = entity.SubComments.Select(subCommentEntity => CommentRetrieveResponse.From(subCommentEntity)).ToList(),
    };
  }

  public static CommentRetrieveResponse[] From(CommentEntity[] entities) {
    return entities.Select(entity => CommentRetrieveResponse.From(entity)).ToArray();
  }
}
