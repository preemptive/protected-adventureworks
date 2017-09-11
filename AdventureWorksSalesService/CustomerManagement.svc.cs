using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace AdventureWorksSalesService
{
    /// <summary> Information for specifying a new Customer entry. </summary>
    [DataContract]
    public class CustomerInfo
    {
        /// <summary> The customers first (given) name. </summary>
        [DataMember(IsRequired = true)] public string FirstName { get; set; }

        /// <summary> The customers last (family) name. </summary>
        [DataMember(IsRequired = true)] public string LastName { get; set; }

        /// <summary> The customer's middle name or initial. </summary>
        [DataMember] public string MiddleName { get; set; }

        /// <summary> The customer's title (e.g., "Dr."). </summary>
        [DataMember] public string Title { get; set; }

        /// <summary> The customer's suffix (e.g., "Jr."). </summary>
        [DataMember] public string Suffix { get; set; }

        /// <summary> Whether the customer opts in to promotional emails. </summary>
        [DataMember] public bool EmailOptIn { get; set; }

        /// <summary>
        /// Whether the customer opts in to promotional emails from
        /// partners of Adventure Works. If this is false but <see cref="EmailOptIn"/>
        /// is true, then the customer will only receive promotional emails sent
        /// by Adventure Works itself.
        /// </summary>
        [DataMember] public bool EmailFromPartners { get; set; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                throw new FaultException("FirstName is not set.");
            }
            if (string.IsNullOrWhiteSpace(LastName))
            {
                throw new FaultException("LastName is not set.");
            }
        }

        internal int EmailCode
        {
            get
            {
                if (EmailOptIn)
                {
                    return EmailFromPartners ? 2 : 1;
                }
                return 0;
            }
        }
    }

    /// <summary> Information for specifying a new Email entry. </summary>
    [DataContract]
    public class EmailInfo
    {
        /// <summary> The e-mail address. </summary>
        [DataMember(IsRequired = true)] public string EmailAddress { get; set; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(EmailAddress))
            {
                throw new FaultException("EmailAddress is not set.");
            }
        }
    }
    
    /// <summary> Information for specifying a new or modified Phone entry. </summary>
    [DataContract]
    public class PhoneInfo
    {
        /// <summary> The phone number. </summary>
        [DataMember(IsRequired = true)] public string PhoneNumber { get; set; }

        /// <summary> 
        /// The kind of phone number. 
        /// Encoded as an integer, see <see cref="PhoneNumberType"/> for meanings. 
        /// </summary>
        [DataMember(IsRequired = true)] public int PhoneNumberTypeId { get; set; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                throw new FaultException("EmailAddress is not set.");
            }
            switch (PhoneNumberTypeId)
            {
                case 1:
                case 2:
                case 3:
                    // valid
                    break;
                default:
                    throw new FaultException($"PhoneNumberTypeId is invalid, was {PhoneNumberTypeId}.");
            }
        }
    }

    /// <summary> Information for specifying a Credit Card entry. </summary>
    [DataContract]
    public class CreditCardInfo
    {
        /// <summary> The brand of card (e.g., ColonialVoice). </summary>
        [DataMember(IsRequired = true)] public string CardType { get; set; }

        /// <summary> The card number. </summary>
        [DataMember(IsRequired = true)] public string CardNumber { get; set; }

        /// <summary> The month the card expires, expressed as a positive integer. </summary>
        [DataMember(IsRequired = true)] public byte ExpMonth { get; set; }

        /// <summary> The four-digit year the card expires. </summary>
        [DataMember(IsRequired = true)] public ushort ExpYear { get; set; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(CardType))
            {
                throw new FaultException("CardType is not set.");
            }
            if (string.IsNullOrWhiteSpace(CardNumber))
            {
                throw new FaultException("CardNumber is not set.");
            }
            if (ExpMonth == 0 || ExpMonth > 12)
            {
                throw new FaultException("ExpMonth is not a valid month number (1-12 inclusive).");
            }
        }
    }

    /// <summary>
    /// Indicates that a request to create a Credit Card failed because the Card Number requested is already in use.
    /// </summary>
    [DataContract]
    [Serializable]
    public class CreditCardNumberInUseFault { }

    /// <summary> Interface for the Customer Management service. </summary>
    [ServiceContract]
    public interface ICustomerManagementService
    {
        /// <summary>
        /// Creates a new record for an individual customer in the Person and BusinessEntity tables.
        /// </summary>
        /// <param name="toCreate">information about the customer</param>
        /// <returns>the BusinessEntityId of the customer</returns>
        [OperationContract]
        int CreateCustomer(CustomerInfo toCreate);

        /// <summary>
        /// Deletes a record for an individual customer in the Person and BusinessEntity tables,
        /// along with associated Phone and Email entries. Credit Cards are deleted only
        /// if they are not shared with any other customers.
        /// </summary>
        /// <param name="businessEntityId">the BusinessEntityId of the customer record to delete</param>
        [OperationContract]
        
        void RemoveCustomer(int businessEntityId);

        /// <summary>
        /// Creates a new record for an individual customer's email address and associates the record with
        /// the customer.
        /// </summary>
        /// <param name="businessEntityId">the BusinessEntityId of the customer</param>
        /// <param name="toCreate">information about the email address</param>
        /// <returns>the EmailAddressId</returns>
        [OperationContract]
        
        int CreateEmailAndAssociateWithCustomer(int businessEntityId, EmailInfo toCreate);

        /// <summary>
        /// Deletes an email record for an individual customer.
        /// </summary>
        /// <param name="emailAddressId">the EmailAddressId</param>
        [OperationContract]
        
        void RemoveEmail(int emailAddressId);

        /// <summary>
        /// Creates a new record for an individual customer's phone number and associates the record with
        /// the customer.
        /// </summary>
        /// <param name="businessEntityId">the BusinessEntityId of the customer</param>
        /// <param name="toCreate">information about the phone number</param>
        [OperationContract]
        
        void CreatePhoneAndAssociateWithCustomer(int businessEntityId, PhoneInfo toCreate);

        /// <summary>
        /// Replaces a customer's phone record with another record.
        /// </summary>
        /// <param name="businessEntityId">the BusinessEntityId of the customer</param>
        /// <param name="oldPhone">information about the phone number to remove</param>
        /// <param name="newPhone">information about the phone number to add</param>
        [OperationContract]
        
        void ReplacePhone(int businessEntityId, PhoneInfo oldPhone, PhoneInfo newPhone);

        /// <summary>
        /// Deletes a phone record for an individual customer.
        /// </summary>
        /// <param name="businessEntityId">the BusinessEntityId of the customer</param>
        /// <param name="toDelete">information about the phone number</param>
        [OperationContract]
        
        void RemovePhone(int businessEntityId, PhoneInfo toDelete);
        
        /// <summary>
        /// Creates a new credit card entry and associates it with a customer.
        /// </summary>
        /// <param name="businessEntityId">the customer's BusinessEntityId</param>
        /// <param name="toCreate">information about the credit card</param>
        /// <returns>the CreditCardId</returns>
        /// <exception cref="FaultException{CreditCardNumberInUseFault}">if the Credit Card Number is already in use</exception>
        [OperationContract]
        
        [FaultContract(typeof(CreditCardNumberInUseFault))]
        int CreateCreditCardAndAssociateWithCustomer(int businessEntityId, CreditCardInfo toCreate);

        /// <summary>
        /// Associates an existing credit card entry with a customer.
        /// </summary>
        /// <param name="businessEntityId">the customer's BusinessEntityId</param>
        /// <param name="creditCardId">the CreditCardId</param>
        [OperationContract]
        
        void AssociateCreditCardWithCustomer(int businessEntityId, int creditCardId);

        /// <summary>
        /// Disassociates an existing credit card entry from a customer.
        /// </summary>
        /// <param name="businessEntityId">the customer's BusinessEntityId</param>
        /// <param name="creditCardId">the CreditCardId</param>
        [OperationContract]
        
        void DisassociateCreditCardFromCustomer(int businessEntityId, int creditCardId);

        /// <summary>
        /// Removes an existing credit card entry and disassociates it from all customers.
        /// </summary>
        /// <param name="creditCardId">the CreditCardId</param>
        [OperationContract]
        
        void RemoveCreditCard(int creditCardId);
    }

    /// <summary>
    /// The Customer Management service for the Adventure Works Sales host.
    /// Creation and deletion of customer-related records (and modification of phone records)
    /// must be done through this service, not <see cref="Data"/>, to ensure the correct relationships
    /// are maintained among records.
    /// </summary>
    /// <remarks> 
    /// NOTE: In order to launch WCF Test Client for testing this service, please select CustomerManagement.svc 
    /// or CustomerManagement.svc.cs at the Solution Explorer and start debugging. 
    /// </remarks>
    [AiLogException]
    public class CustomerManagement : ICustomerManagementService
    {
        private void ValidateNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new FaultException($"{name} must not be negative, was {value}.");
            }
        }

        private void DoSql(string commandText, params KeyValuePair<SqlParameter, object>[] parameters)
        {
            doSql(commandText, parameters, command =>
            {
                command.ExecuteNonQuery();
            });
        }
        
        private T DoSql<T>(bool commandAllowedToReturnDbNull, string commandText, params KeyValuePair<SqlParameter, object>[] parameters) 
        {
            T result = default(T);
            bool resultSet = false;
            doSql(commandText, parameters, command =>
            {
                var dbResult = command.ExecuteScalar();
                if (dbResult == DBNull.Value)
                {
                    if (!commandAllowedToReturnDbNull)
                    {
                        throw new Exception("SQL command returned DBNull, which is not allowed for this command");
                    }
                    result = default(T);
                    resultSet = true;
                }
                else
                {
                    result = (T) dbResult;
                    resultSet = true;
                }
            });
            if (resultSet)
            {
                return result;
            }
            throw new Exception("SQL command did not return result");
        }

        private void doSql(string commandText, KeyValuePair<SqlParameter, object>[] parameters, Action<SqlCommand> operation)
        {
            var setting = ConfigurationManager.ConnectionStrings["CustomerManagement"];
            using (var sql = new SqlConnection(setting.ConnectionString))
            {
                sql.Open();

                using (var command = new SqlCommand(commandText, sql))
                {
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(parameter.Key).Value = parameter.Value;
                    }

                    operation(command);
                }
            }
        }

        public int CreateCustomer(CustomerInfo toCreate)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            toCreate.Validate();

            return DoSql<int>(false,
                @"SET XACT_ABORT ON;

DECLARE @NewEntityId int;

BEGIN TRANSACTION

    INSERT INTO [Person].[BusinessEntity]
    DEFAULT VALUES

    SET @NewEntityId = SCOPE_IDENTITY();

    INSERT INTO[Person].[Person]
        ([BusinessEntityID]
        ,[PersonType]
        ,[NameStyle]
        ,[Title]
        ,[FirstName]
        ,[MiddleName]
        ,[LastName]
        ,[Suffix]
        ,[EmailPromotion]
        ,[AdditionalContactInfo]
        ,[Demographics])
    VALUES
        (@NewEntityId
        ,'IN'
        ,0
        ,@name_title
        ,@name_first
        ,@name_middle
        ,@name_last
        ,@name_suffix
        ,@email_pref
        , NULL
        , NULL)

COMMIT

SELECT @NewEntityId",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@name_title", SqlDbType.NVarChar, 8), (object)toCreate.Title ?? DBNull.Value),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@name_first", SqlDbType.NVarChar, 50), toCreate.FirstName),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@name_middle", SqlDbType.NVarChar, 50), (object)toCreate.MiddleName ?? DBNull.Value),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@name_last", SqlDbType.NVarChar, 50), toCreate.LastName),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@name_suffix", SqlDbType.NVarChar, 10), (object)toCreate.Suffix ?? DBNull.Value),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("email_pref", SqlDbType.Int), toCreate.EmailCode)
            );
        }

        public void RemoveCustomer(int businessEntityId)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(businessEntityId, nameof(businessEntityId));

            DoSql(@"SET XACT_ABORT ON;

BEGIN TRANSACTION
    
	DELETE
	FROM [Person].[Password]
	WHERE BusinessEntityID = @id_to_delete

	DELETE
	FROM [Person].[BusinessEntityAddress]
	WHERE BusinessEntityID = @id_to_delete

	DELETE
	FROM [Sales].[SalesOrderHeader]
	WHERE EXISTS
	(
        SELECT CustomerID
        FROM [Sales].[Customer]
        WHERE [Sales].[SalesOrderHeader].CustomerID = [Sales].[Customer].CustomerID
		  AND [Sales].[Customer].PersonID = @id_to_delete
	)

    DELETE
    FROM [Sales].[Customer]
    WHERE PersonID = @id_to_delete

    DELETE
    FROM [Sales].[PersonCreditCard]
    WHERE BusinessEntityID = @id_to_delete

    DELETE
    FROM [Sales].[SalesOrderHeader]
    WHERE NOT EXISTS 
    (
        SELECT CreditCardID 
        FROM [Sales].[PersonCreditCard]
        WHERE [Sales].[SalesOrderHeader].CreditCardID = [Sales].[PersonCreditCard].CreditCardID
    )

    DELETE
    FROM [Sales].[CreditCard]
    WHERE NOT EXISTS 
    (
        SELECT CreditCardID 
        FROM [Sales].[PersonCreditCard]
        WHERE [Sales].[CreditCard].CreditCardID = [Sales].[PersonCreditCard].CreditCardID
    )

    DELETE FROM [Person].[PersonPhone]
    WHERE BusinessEntityID = @id_to_delete

    DELETE FROM [Person].[EmailAddress]
    WHERE BusinessEntityID = @id_to_delete

    DELETE FROM [Person].[Person]
    WHERE BusinessEntityID = @id_to_delete

    DELETE FROM [Person].[BusinessEntity]
    WHERE BusinessEntityID = @id_to_delete

COMMIT", 
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@id_to_delete", SqlDbType.Int), businessEntityId)
            );
        }

        public int CreateEmailAndAssociateWithCustomer(int businessEntityId, EmailInfo toCreate)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(businessEntityId, nameof(businessEntityId));
            toCreate.Validate();

            return DoSql<int>(false, 
                @"SET XACT_ABORT ON;

DECLARE @InsertedRecord table ( EmailAddressID int )

BEGIN TRANSACTION

    INSERT  INTO [Person].[EmailAddress]
			    ([BusinessEntityID]
			    ,[EmailAddress])
	        OUTPUT INSERTED.EmailAddressID INTO @InsertedRecord
	        VALUES
			    (@customerBusinessEntityId
			    ,@newEmail)

COMMIT

SELECT EmailAddressID FROM @InsertedRecord",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@customerBusinessEntityId", SqlDbType.Int), businessEntityId),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@newEmail", SqlDbType.NVarChar, 50), toCreate.EmailAddress)
            );
        }

        public void RemoveEmail(int emailAddressId)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(emailAddressId, nameof(emailAddressId));

            DoSql(@"SET XACT_ABORT ON;

BEGIN TRANSACTION
	
    DELETE FROM [Person].[EmailAddress]
    WHERE EmailAddressID = @id_to_delete

COMMIT",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@id_to_delete", SqlDbType.Int), emailAddressId)
            );
        }

        public void CreatePhoneAndAssociateWithCustomer(int businessEntityId, PhoneInfo toCreate)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(businessEntityId, nameof(businessEntityId));
            toCreate.Validate();

            DoSql(@"SET XACT_ABORT ON;

BEGIN TRANSACTION

    INSERT INTO [Person].[PersonPhone]
			    ([BusinessEntityID]
			    ,[PhoneNumber]
                ,[PhoneNumberTypeID])
		    VALUES
			    (@customerBusinessEntityId
			    ,@newPhone
                ,@newPhoneType)

COMMIT",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@customerBusinessEntityId", SqlDbType.Int), businessEntityId),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@newPhone", SqlDbType.NVarChar, 25), toCreate.PhoneNumber),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@newPhoneType", SqlDbType.Int), toCreate.PhoneNumberTypeId)
            );
        }

        public void ReplacePhone(int businessEntityId, PhoneInfo oldPhone, PhoneInfo newPhone)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(businessEntityId, nameof(businessEntityId));
            oldPhone.Validate();
            newPhone.Validate();

            DoSql(@"SET XACT_ABORT ON;

BEGIN TRANSACTION

    DELETE FROM [Person].[PersonPhone]
    WHERE BusinessEntityID = @customerBusinessEntityId
        AND PhoneNumber = @oldPhone
        AND PhoneNumberTypeID = @oldPhoneType

    INSERT INTO [Person].[PersonPhone]
			    ([BusinessEntityID]
			    ,[PhoneNumber]
                ,[PhoneNumberTypeID])
		    VALUES
			    (@customerBusinessEntityId
			    ,@newPhone
                ,@newPhoneType)

COMMIT",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@customerBusinessEntityId", SqlDbType.Int), businessEntityId),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@oldPhone", SqlDbType.NVarChar, 25), oldPhone.PhoneNumber),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@oldPhoneType", SqlDbType.Int), oldPhone.PhoneNumberTypeId),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@newPhone", SqlDbType.NVarChar, 25), newPhone.PhoneNumber),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@newPhoneType", SqlDbType.Int), newPhone.PhoneNumberTypeId)
            );
        }

        public void RemovePhone(int businessEntityId, PhoneInfo toDelete)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(businessEntityId, nameof(businessEntityId));
            toDelete.Validate();

            DoSql(@"SET XACT_ABORT ON;

BEGIN TRANSACTION

    DELETE FROM [Person].[PersonPhone]
    WHERE BusinessEntityID = @customerBusinessEntityId
        AND PhoneNumber = @oldPhone
        AND PhoneNumberTypeID = @oldPhoneType

COMMIT",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@customerBusinessEntityId", SqlDbType.Int), businessEntityId),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@oldPhone", SqlDbType.NVarChar, 25), toDelete.PhoneNumber),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@oldPhoneType", SqlDbType.Int), toDelete.PhoneNumberTypeId)
            );
        }

        public void AssociateCreditCardWithCustomer(int businessEntityId, int creditCardId)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(businessEntityId, nameof(businessEntityId));
            ValidateNonNegative(creditCardId, nameof(creditCardId));

            DoSql(
                @"SET XACT_ABORT ON;

BEGIN TRANSACTION

    INSERT INTO [Sales].[PersonCreditCard]
		    ([BusinessEntityID]
		    ,[CreditCardID])
    VALUES
            (@customerBusinessEntityId
            ,@creditCardId)


COMMIT",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@customerBusinessEntityId", SqlDbType.Int), businessEntityId),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@creditCardId", SqlDbType.Int), creditCardId)
            );
        }

        public int CreateCreditCardAndAssociateWithCustomer(int businessEntityId, CreditCardInfo toCreate)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(businessEntityId, nameof(businessEntityId));
            toCreate.Validate();

            var result = DoSql<int?>(true,
                @"SET XACT_ABORT ON;

DECLARE @InsertedRecord table ( CreditCardID int )
DECLARE @NewCreditCardId int;

BEGIN TRANSACTION

		IF 0 = (
			    SELECT COUNT(*)
				FROM [Sales].[CreditCard]
				WHERE [CardNumber] = @cardNumber
			)
		BEGIN
			INSERT INTO [Sales].[CreditCard]
				([CardType]
				,[CardNumber]
				,[ExpMonth]
				,[ExpYear])
			OUTPUT INSERTED.CreditCardID INTO @InsertedRecord
			VALUES
				(@cardType
				,@cardNumber
				,@cardExpMonth
				,@cardExpYear)

		    SELECT @NewCreditCardId = r.CreditCardID
		    FROM @InsertedRecord r

            INSERT INTO [Sales].[PersonCreditCard]
				    ([BusinessEntityID]
				    ,[CreditCardID])
            VALUES
                    (@customerBusinessEntityId
                    ,@NewCreditCardId)
	    END

COMMIT

SELECT @NewCreditCardId",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@customerBusinessEntityId", SqlDbType.Int), businessEntityId),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@cardType", SqlDbType.NVarChar, 50), toCreate.CardType),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@cardNumber", SqlDbType.NVarChar, 25), toCreate.CardNumber),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@cardExpMonth", SqlDbType.TinyInt), toCreate.ExpMonth),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@cardExpYear", SqlDbType.SmallInt), toCreate.ExpYear)
            );

            if (result.HasValue)
            {
                return result.Value;
            }

            throw new FaultException<CreditCardNumberInUseFault>(
                new CreditCardNumberInUseFault(),
                "The Credit Card Number specified is already in use.");
        }

        public void DisassociateCreditCardFromCustomer(int businessEntityId, int creditCardId)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(businessEntityId, nameof(businessEntityId));
            ValidateNonNegative(creditCardId, nameof(creditCardId));

            DoSql(
                @"SET XACT_ABORT ON;

BEGIN TRANSACTION

    DELETE FROM [Sales].[PersonCreditCard]
    WHERE BusinessEntityID = @customerBusinessEntityId
        AND CreditCardID = @creditCardId

    DELETE
    FROM [Sales].[SalesOrderHeader]
    WHERE NOT EXISTS 
    (
        SELECT CreditCardID 
        FROM [Sales].[PersonCreditCard]
        WHERE [Sales].[SalesOrderHeader].CreditCardID = [Sales].[PersonCreditCard].CreditCardID
    )

    DELETE
    FROM [Sales].[CreditCard]
    WHERE NOT EXISTS 
    (
        SELECT CreditCardID 
        FROM [Sales].[PersonCreditCard]
        WHERE [Sales].[CreditCard].CreditCardID = [Sales].[PersonCreditCard].CreditCardID
    )

COMMIT",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@customerBusinessEntityId", SqlDbType.Int), businessEntityId),
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@creditCardId", SqlDbType.Int), creditCardId)
            );
        }

        public void RemoveCreditCard(int creditCardId)
        {
            Auth.ThrowFaultExceptionIfNotAuthenticated();
            ValidateNonNegative(creditCardId, nameof(creditCardId));
            
            DoSql(
                @"SET XACT_ABORT ON;

BEGIN TRANSACTION

    DELETE FROM [Sales].[SalesOrderHeader]
    WHERE CreditCardID = @creditCardId

    DELETE FROM [Sales].[PersonCreditCard]
    WHERE CreditCardID = @creditCardId

    DELETE FROM [Sales].[CreditCard]
    WHERE CreditCardID = @creditCardId

COMMIT",
                new KeyValuePair<SqlParameter, object>(new SqlParameter("@creditCardId", SqlDbType.Int), creditCardId)
            );
        }
    }
}
