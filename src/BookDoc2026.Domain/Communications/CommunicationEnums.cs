namespace BookDoc2026.Domain.Communications;

public enum CommunicationChannel
{
    Email = 1,
    Sms = 2,
    WhatsApp = 3,
    Push = 4
}

public enum MessageTemplateContentKind
{
    PlainText = 1,
    Html = 2
}

public enum MessageTemplateStatus
{
    Draft = 1,
    Published = 2,
    Retired = 3
}

public enum MessageDeliveryStatus
{
    Accepted = 1,
    TransientFailure = 2,
    PermanentFailure = 3
}

public enum CommunicationPreferenceDecision
{
    Allowed = 1,
    Denied = 2,
    Withdrawn = 3
}

public enum CommunicationMessageClass
{
    Transactional = 1,
    Clinical = 2,
    Marketing = 3
}

public enum ProviderCallbackInboxStatus
{
    Pending = 1,
    Processed = 2,
    Failed = 3
}

public enum ProviderDeliveryStatus
{
    Delivered = 1,
    Failed = 2,
    Bounced = 3
}
